// <copyright file="RouteProblem.cs" company="Carson Page">
// Copyright (c) Carson Page. All rights reserved.
// </copyright>

namespace FSEcoRouteSolver
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using CsvHelper;
    using Google.OrTools.ConstraintSolver;
    using Google.Protobuf.WellKnownTypes;
    using SharpKml.Base;
    using SharpKml.Dom;
    using SharpKml.Engine;

    internal class RouteProblem
    {
        private const long ASSIGNMENTCOUNTMAX = 100;
        private readonly List<Node> nodes;
        private readonly List<(int, int)> pdpPairs;
        private readonly RoutingModel model;
        private readonly RoutingIndexManager manager;
        private readonly RoutingProblemParameters parameters;

        // private readonly List<GreatCircleDistance> costEvaluators;

        /// <summary>
        /// Initializes a new instance of the <see cref="RouteProblem"/> class.
        /// </summary>
        /// <param name="parameters">The routing Parameters.</param>
        /// <param name="api_key">FSE data feed API key.</param>
        public RouteProblem(RoutingProblemParameters parameters, string api_key)
        {
            this.parameters = parameters;
            var hub = this.parameters.HubICAO;
            var fleet = this.parameters.Fleet;

            var icaodata = new CsvReader(File.OpenText(@"./icaodata.csv"));
            var icaodata_records = icaodata.GetRecords<IcaoDataRecord>().ToDictionary(x => x.icao, x => x);
            var webClient = new WebClient();
            var to_jobs = new CsvReader(new StringReader(webClient.DownloadString(string.Format(@"http://server.fseconomy.net/data?userkey={0}&format=csv&query=icao&search=jobsto&icaos={1}", api_key, hub))));
            var from_jobs = new CsvReader(new StringReader(webClient.DownloadString(string.Format(@"http://server.fseconomy.net/data?userkey={0}&format=csv&query=icao&search=jobsfrom&icaos={1}", api_key, hub))));

            this.nodes = new List<Node>();
            this.pdpPairs = new List<(int, int)>();

            // "Depot" Node
            this.nodes.Add(new Node
            {
                Name = hub,
                Demand = 0,
                Lat = icaodata_records[hub].lat,
                Lon = icaodata_records[hub].lon,
                Pay = 0,
                Commodity = "Root",
            });

            foreach (var x in to_jobs.GetRecords<JobRecord>())
            {
                if (x.PtAssignment == true)
                {
                    var not_hub = x.FromIcao.ToUpper();

                    this.nodes.Add(new Node
                    {
                        Name = not_hub,
                        Demand = x.Amount,
                        Pay = 0,
                        Lat = icaodata_records[not_hub].lat,
                        Lon = icaodata_records[not_hub].lon,
                        AssignmentId = x.Id,
                        Commodity = x.Commodity,
                    });

                    this.nodes.Add(new Node
                    {
                        Name = hub,
                        Demand = -x.Amount,
                        Pay = (int)Math.Round(x.Pay * 100),
                        Lat = icaodata_records[hub].lat,
                        Lon = icaodata_records[hub].lon,
                        AssignmentId = x.Id,
                        Commodity = x.Commodity,
                    });

                    this.pdpPairs.Add((this.nodes.Count - 2, this.nodes.Count - 1));
                }
            }

            foreach (var x in from_jobs.GetRecords<JobRecord>())
            {
                if (x.PtAssignment == true)
                {
                    var not_hub = x.ToIcao.ToUpper();

                    this.nodes.Add(new Node
                    {
                        Name = hub,
                        Demand = x.Amount,
                        Pay = 0,
                        Lat = icaodata_records[hub].lat,
                        Lon = icaodata_records[hub].lon,
                        AssignmentId = x.Id,
                        Commodity = x.Commodity,
                    });
                    this.nodes.Add(new Node
                    {
                        Name = not_hub,
                        Demand = -x.Amount,
                        Pay = (int)Math.Round(x.Pay * 100),
                        Lat = icaodata_records[not_hub].lat,
                        Lon = icaodata_records[not_hub].lon,
                        AssignmentId = x.Id,
                        Commodity = x.Commodity,
                    });

                    this.pdpPairs.Add((this.nodes.Count - 2, this.nodes.Count - 1));
                }
            }

            this.manager = new RoutingIndexManager(this.nodes.Count, fleet.Count, 0);
            this.model = new RoutingModel(this.manager);

            this.SetupDimensions();
        }

        public void EnableBookingFee()
        {
            var assignment_count = this.model.GetMutableDimension("assignment_count");

            // var bfCalls = paxCapacities.Select(c => this.model.RegisterUnaryTransitCallback(
            //     (long index) => -c)).ToArray();

            // this.model.AddDimensionWithVehicleTransitAndCapacity(bfCalls, maxCapacity * 2, paxCapacities, true, "booking_fee");

            this.model.AddConstantDimensionWithSlack(-ASSIGNMENTCOUNTMAX, ASSIGNMENTCOUNTMAX, 2 * ASSIGNMENTCOUNTMAX, false, "booking_fee");

            var booking_fee = this.model.GetMutableDimension("booking_fee");
            var solver = this.model.solver();

            //var totalPay = this.nodes.Select(n => n.Pay).Sum();
            //this.model.AddConstantDimensionWithSlack(0, totalPay, totalPay, true, "bf_prime");
            //var bfPrime = this.model.GetMutableDimension("bf_prime");

            for (int i = 0; i < this.nodes.Count; i++)
            {
                var ii = this.manager.NodeToIndex(i);

                var is_lt_five = assignment_count.CumulVar(ii) < 5;
                var not_is_lt_five = (1 - is_lt_five).Var();

                var must_be_gt = booking_fee.CumulVar(ii) >= assignment_count.CumulVar(ii);
                solver.Add(solver.MakeConditionalExpression(not_is_lt_five, must_be_gt, 1) == 1);

                if (this.nodes[i].Demand < 0)
                {
                    var not_nnz = assignment_count.CumulVar(ii) > 1;
                    var force_nochange = solver.MakeConditionalExpression(not_nnz, booking_fee.SlackVar(ii), ASSIGNMENTCOUNTMAX);
                    solver.Add(force_nochange == ASSIGNMENTCOUNTMAX);
                }
                else
                {
                    booking_fee.SlackVar(ii).RemoveInterval(0, ASSIGNMENTCOUNTMAX - 1);
                }

                this.model.AddVariableMinimizedByFinalizer(booking_fee.CumulVar(ii));
            }
        }

        public string Solve()
        {
            var solver = this.model.solver();
            var fleet = this.parameters.Fleet;

            var distance = this.model.GetMutableDimension("distance");
            foreach (var pair in this.pdpPairs)
            {
                (var pickup, var delivery) = pair;
                var pickup_idx = this.manager.NodeToIndex(pickup);
                var delivery_idx = this.manager.NodeToIndex(delivery);

                // this.model.AddPickupAndDelivery(pickup_idx, delivery_idx);
                solver.Add(distance.CumulVar(pickup_idx) <= distance.CumulVar(delivery_idx));
                solver.Add(this.model.VehicleVar(pickup_idx) == this.model.VehicleVar(delivery_idx));

                // this.model.AddDisjunction(new long[] { pickup_idx, delivery_idx }, this.nodes[delivery].Pay, 2);
                var pickup_didx = this.model.AddDisjunction(new long[] { pickup_idx }, this.nodes[delivery].Pay / 2);
                var delivery_didx = this.model.AddDisjunction(new long[] { delivery_idx }, this.nodes[delivery].Pay / 2);

                this.model.AddPickupAndDeliverySets(pickup_didx, delivery_didx);
            }

            this.model.SetPickupAndDeliveryPolicyOfAllVehicles(RoutingModel.PICKUP_AND_DELIVERY_NO_ORDER);

            var routingParameters = operations_research_constraint_solver.DefaultRoutingSearchParameters();
            routingParameters.LocalSearchMetaheuristic = LocalSearchMetaheuristic.Types.Value.GuidedLocalSearch;
            routingParameters.FirstSolutionStrategy = FirstSolutionStrategy.Types.Value.ParallelCheapestInsertion;
            routingParameters.LogSearch = true;
            routingParameters.TimeLimit = new Duration { Seconds = this.parameters.MaxSolveSec };
            var solution = this.model.SolveWithParameters(routingParameters);

            return this.PrintSolution(solution);
        }

        private string PrintSolution(in Assignment solution)
        {
            // Inspect solution.
            long totalDistance = 0;
            long totalPay = 0;
            string output = string.Empty;
            var distance = this.model.GetMutableDimension("distance");
            // var booking_fee = this.model.GetDimensionOrDie("booking_fee");
            // var assignment_count = this.model.GetMutableDimension("assignment_count");
            Document doc = new Document
            {
                Id = "Document",
            };
            for (int i = 0; i < this.parameters.Fleet.Count; ++i)
            {
                var routeAirport = new Folder
                {
                    Id = string.Format("airport-{0}", i),
                    Name = string.Format("Solution Airports {0}", i),
                };

                output += string.Format("Route for Aircraft {0}:\n", i);
                long routePay = 0;
                var index = this.model.Start(i);
                while (this.model.IsEnd(index) == false)
                {
                    var nodeIndex = this.manager.IndexToNode(index);
                    var node = this.nodes[nodeIndex];
                    var nodeId = node.Name + "-" + i;
                    var local_f = routeAirport.FindFeature(nodeId);
                    if (local_f == null)
                    {
                        var placemark = new Placemark
                        {
                            Id = nodeId,
                            Name = node.Name,
                            Geometry = new Point
                            {
                                Coordinate = new Vector(node.Lat, node.Lon),
                            },
                        };
                        routeAirport.AddFeature(placemark);
                    }


                    // long bf_l, bf_u;
                    // long thing = solution.Value(booking_fee.CumulVar(index));
                    // output += string.Format("Booking Fee: {0}\n", thing);
                    // output += string.Format("Assignment Count: {0}\n", solution.Value(assignment_count.CumulVar(index)));
                    if (node.Demand > 0)
                    {
                        var delivery_node = this.nodes[nodeIndex + 1];
                        output += string.Format("Pickup: {0} -->{3}: {1}x {2}\n", node.Name, node.Demand, node.Commodity, delivery_node.Name);
                    }
                    else
                    {
                        output += string.Format("Deliver: {3} {1}x {2}, Pay: ${0}\n", node.Pay / 100, Math.Abs(node.Demand), node.Commodity, node.Name);
                        routePay += node.Pay / 100;
                    }

                    index = solution.Value(this.model.NextVar(index));
                }

                var distanceVar = distance.CumulVar(index);
                long routeDistance = solution.Value(distanceVar) / 100;
                output += string.Format("-------------------------\n");
                output += string.Format("Distance of the route: {0} NM\n", solution.Value(distanceVar) / 100);
                output += string.Format("Gross pay of the route: ${0}\n", (double)routePay);
                output += "\n\n\n";
                totalDistance += routeDistance;
                totalPay += routePay;
                doc.AddFeature(routeAirport);
            }

            KmlFile kml = KmlFile.Create(doc, true);
            using (FileStream stream = File.Create("output.kml"))
            {
                kml.Save(stream);
            }

            output += string.Format("Total distance of all routes: {0} NM\n", totalDistance);
            output += string.Format("Total gross pay of all routes: ${0}\n", (double)totalPay);

            return output;
        }

        private void SetupDimensions()
        {
            // Count
            var countCall = this.model.RegisterUnaryTransitCallback((from_idx) =>
            {
                if (this.nodes[this.manager.IndexToNode(from_idx)].Demand >= 0)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            });
            this.model.AddDimension(countCall, 0, ASSIGNMENTCOUNTMAX, true, "count");

            var fleet = this.parameters.Fleet;

            var paxCapacities = fleet.Select(a => (long)a.Passengers).ToArray();

            var costEvalulators = this.parameters.Fleet
                .Select(a => new GreatCircleDistance(this.manager, a.CostPerNMCents(4.19), this.nodes)).ToList();

            for (int i = 0; i < costEvalulators.Count; i++)
            {
                var call = this.model.RegisterTransitCallback(costEvalulators[i].Call);
                this.model.SetArcCostEvaluatorOfVehicle(call, i);
            }

            var distanceCall = new GreatCircleDistance(this.manager, 100, this.nodes);
            this.model.AddDimension(this.model.RegisterTransitCallback(distanceCall.Call), 0, this.parameters.MaxLength, true, "distance");

            var timeEvaluators = new List<TimeEvaluator>();
            for (int i = 0; i < fleet.Count; i++)
            {
                var timeCall = new TimeEvaluator(costEvalulators[i], this.manager, fleet[i].CruiseSpeed, this.nodes);
                timeEvaluators.Add(timeCall);
            }

            this.model.AddDimensionWithVehicleTransits(
                timeEvaluators.Select(e => this.model.RegisterTransitCallback(e.Call)).ToArray(),
                0,
                this.parameters.MaxTimeEnroute,
                true,
                "time");

            int assignmentCallBackIndex = this.model.RegisterUnaryTransitCallback(
                (long fromIndex) =>
                {
                    var fromNode = this.manager.IndexToNode(fromIndex);
                    return Math.Sign(this.nodes[fromNode].Demand);
                });

            /*
            this.model.AddDimensionWithVehicleCapacity(
              assignmentCallBackIndex,
              0,  // null capacity slack
              paxCapacities,   // vehicle maximum capacities
              true,                      // start cumul to zero
              "assignment_count");
            var assignmentCount = this.model.GetMutableDimension("assignment_count");
            */

            this.model.AddDimension(assignmentCallBackIndex, 0, ASSIGNMENTCOUNTMAX, true, "assignment_count");

            int demandCallbackIndex = this.model.RegisterUnaryTransitCallback(
                (long fromIndex) =>
                {
                    // Convert from routing variable Index to demand NodeIndex.
                    var fromNode = this.manager.IndexToNode(fromIndex);
                    return this.nodes[fromNode].Demand;
                });

            this.model.AddDimensionWithVehicleCapacity(
              demandCallbackIndex,
              0,  // null capacity slack
              paxCapacities,   // vehicle maximum capacities
              true,                      // start cumul to zero
              "capacity");

            var capacity = this.model.GetMutableDimension("capacity");
        }

        private class Node
        {
            public string Name { get; set; }

            public double Lat { get; set; }

            public double Lon { get; set; }

            public int Demand { get; set; }

            public int AssignmentId { get; set; }

            public int Pay { get; set; }

            public string Commodity { get; set; }
        }

        private class JobRecord
        {
            public string FromIcao { get; set; }

            public string ToIcao { get; set; }

            public int Amount { get; set; }

            public decimal Pay { get; set; }

            public bool PtAssignment { get; set; }

            public string Commodity { get; set; }

            public int Id { get; set; }
        }

#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Element should begin with upper-case letter
        private class IcaoDataRecord
        {
            public string icao { get; set; }

            public double lat { get; set; }

            public double lon { get; set; }
        }
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore IDE1006

        private class GreatCircleDistance
        {
            private readonly RoutingIndexManager manager;

            public GreatCircleDistance(RoutingIndexManager manager, long cpnm, List<Node> nodes)
            {
                this.manager = manager;
                this.CostMatrix = new long[nodes.Count, nodes.Count];
                for (var i = 0; i < nodes.Count; i++)
                {
                    for (var j = 0; j < nodes.Count; j++)
                    {
                        if (nodes[i].Name == nodes[j].Name)
                        {
                            this.CostMatrix[i, j] = 0;
                        }
                        else
                        {
                            this.CostMatrix[i, j] = (long)Haversine.Calculate(nodes[i].Lat, nodes[i].Lon, nodes[j].Lat, nodes[j].Lon) * cpnm;
                        }
                    }
                }
            }

            public long[,] CostMatrix { get; private set; }

            public long Call(long from_idx, long to_idx)
            {
                var from = this.manager.IndexToNode(from_idx);
                var to = this.manager.IndexToNode(to_idx);
                return this.CostMatrix[from, to];
            }
        }

        private class TimeEvaluator
        {
            private readonly RoutingIndexManager manager;
            private readonly long[,] timeMatrix;

            public TimeEvaluator(GreatCircleDistance distance, RoutingIndexManager manager, long spd, List<Node> nodes)
            {
                this.manager = manager;
                this.timeMatrix = new long[nodes.Count, nodes.Count];
                for (var i = 0; i < nodes.Count; i++)
                {
                    for (var j = 0; j < nodes.Count; j++)
                    {
                        this.timeMatrix[i, j] = distance.CostMatrix[i, j] / spd;
                    }
                }
            }

            public long Call(long from_idx, long to_idx)
            {
                var from = this.manager.IndexToNode(from_idx);
                var to = this.manager.IndexToNode(to_idx);
                return this.timeMatrix[from, to];
            }
        }
    }
}

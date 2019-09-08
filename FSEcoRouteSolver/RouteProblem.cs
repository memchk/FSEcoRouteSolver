﻿// <copyright file="RouteProblem.cs" company="Carson Page">
// Copyright (c) Carson Page. All rights reserved.
// </copyright>

namespace FSEcoRouteSolver
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Diagnostics;
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

            this.model.AddConstantDimensionWithSlack(-ASSIGNMENTCOUNTMAX, ASSIGNMENTCOUNTMAX, 2 * ASSIGNMENTCOUNTMAX, false, "booking_fee");

            var booking_fee = this.model.GetMutableDimension("booking_fee");
            var solver = this.model.solver();

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

            var distance = this.model.GetMutableDimension("distance");
            var node_count = this.model.GetMutableDimension("node_count");
            foreach (var pair in this.pdpPairs)
            {
                (var pickup, var delivery) = pair;
                var pickup_idx = this.manager.NodeToIndex(pickup);
                var delivery_idx = this.manager.NodeToIndex(delivery);

                // this.model.AddPickupAndDelivery(pickup_idx, delivery_idx);
                solver.Add(distance.CumulVar(pickup_idx) <= distance.CumulVar(delivery_idx));
                solver.Add(this.model.VehicleVar(pickup_idx) == this.model.VehicleVar(delivery_idx));

                //this.model.AddDisjunction(new long[] { pickup_idx, delivery_idx }, this.nodes[delivery].Pay, 2);

                var pickup_didx = this.model.AddDisjunction(new long[] { pickup_idx }, this.nodes[delivery].Pay);
                var delivery_didx = this.model.AddDisjunction(new long[] { delivery_idx }, this.nodes[delivery].Pay);
                //this.model.AddPickupAndDelivery(pickup_idx, delivery_idx);
                this.model.AddPickupAndDeliverySets(pickup_didx, delivery_didx);
            }

            var routingParameters = operations_research_constraint_solver.DefaultRoutingSearchParameters();
            routingParameters.LocalSearchMetaheuristic = LocalSearchMetaheuristic.Types.Value.GuidedLocalSearch;
            routingParameters.FirstSolutionStrategy = FirstSolutionStrategy.Types.Value.ParallelCheapestInsertion;

            routingParameters.LogSearch = true;
            routingParameters.TimeLimit = new Duration { Seconds = this.parameters.MaxSolveSec };
            var solution = this.model.SolveWithParameters(routingParameters);

            this.PrintSolutionKML(solution);
            return this.PrintSolution(solution);
        }

        private Node GetNodeFromIndex(long index)
        {
            var idx = this.manager.IndexToNode(index);
            return this.nodes[idx];
        }

        private bool AddAirportToFolder(Folder folder, Node n)
        {
            var airportId = n.Name;
            var kmlAirport = folder.FindFeature(airportId);
            if (kmlAirport == null)
            {
                var placemark = new Placemark
                {
                    Id = airportId,
                    Name = n.Name,
                    Geometry = new Point
                    {
                        Coordinate = new Vector(n.Lat, n.Lon),
                    },
                };
                folder.AddFeature(placemark);
                return true;
            }

            return false;
        }

        private Placemark NewTour(int tourId)
        {
            var tourPath = new LineString()
            {
                AltitudeMode = AltitudeMode.ClampToGround,
                Tessellate = true,
                Extrude = true,
                Coordinates = new CoordinateCollection(),
            };

            return new Placemark
            {
                Name = string.Format("Tour {0}", tourId),
                Geometry = tourPath,
            };
        }

        private void PrintSolutionKML(in Assignment solution)
        {
            Document doc = new Document
            {
                Id = "Document",
            };

            for (int veh = 0; veh < this.parameters.Fleet.Count; ++veh)
            {
                var routeAirportFolder = new Folder
                {
                    Id = string.Format("airport-{0}", veh),
                    Name = string.Format("Solution Airports for Aircraft {0}", veh),
                };

                var hub = this.parameters.HubICAO;
                var tourCount = 0;

                var index = this.model.Start(veh);
                var tourPlacemark = this.NewTour(tourCount);
                var tourPath = (LineString)tourPlacemark.Geometry;

                Node prevNode = null;

                while (this.model.IsEnd(index) == false)
                {
                    var node = this.GetNodeFromIndex(index);
                    this.AddAirportToFolder(routeAirportFolder, node);

                    if (prevNode == null)
                    {
                        tourPath.Coordinates.Add(new Vector(node.Lat, node.Lon));
                    }
                    else
                    {
                        if (prevNode != node && prevNode.Name != node.Name)
                        {
                            tourPath.Coordinates.Add(new Vector(node.Lat, node.Lon));
                        }

                        if (node.Name == hub && prevNode.Name != hub)
                        {
                            routeAirportFolder.AddFeature(tourPlacemark);
                            tourPlacemark = this.NewTour(++tourCount);

                            tourPath = (LineString)tourPlacemark.Geometry;
                            tourPath.Coordinates.Add(new Vector(node.Lat, node.Lon));
                        }
                    }

                    index = solution.Value(this.model.NextVar(index));
                    prevNode = node;
                }

                // At the depot, finish up current tour if it was a one-off
                var end_node = this.GetNodeFromIndex(index);
                if (tourPath.Coordinates.Count > 0)
                {
                    tourPath.Coordinates.Add(new Vector(end_node.Lat, end_node.Lon));
                    routeAirportFolder.AddFeature(tourPlacemark);
                }

                doc.AddFeature(routeAirportFolder);
            }

            KmlFile kml = KmlFile.Create(doc, true);
            var kmlPath = Path.GetFullPath("output.kml");
            using (FileStream stream = File.Create(kmlPath))
            {
                kml.Save(stream);
                Process.Start(kmlPath);
            }
        }

        private string PrintSolution(in Assignment solution)
        {
            // Inspect solution.
            long totalDistance = 0;
            long totalPay = 0;
            string output = string.Empty;
            var hub = this.parameters.HubICAO;
            var distance = this.model.GetMutableDimension("distance");
            var add_assign = @"http://server.fseconomy.net/userctl?event=Assignment&type=add&returnpage=/myflight.jsp&addToGroup=0";

            // var booking_fee = this.model.GetDimensionOrDie("booking_fee");
            // var assignment_count = this.model.GetMutableDimension("assignment_count");
            for (int i = 0; i < this.parameters.Fleet.Count; ++i)
            {
                output += string.Format("Route for Aircraft {0}:\n", i);
                long routePay = 0;
                var index = this.model.Start(i);

                var route_assign = add_assign.Clone();

                while (this.model.IsEnd(index) == false)
                {
                    var nodeIndex = this.manager.IndexToNode(index);
                    var node = this.nodes[nodeIndex];

                    // If is not depot add assignment
                    if (nodeIndex != 0)
                    {
                        route_assign += string.Format(@"&select={0}", node.AssignmentId);
                    }

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

                output += string.Format("Return to hub: {0}", hub);

                var distanceVar = distance.CumulVar(index);
                long routeDistance = solution.Value(distanceVar) / 100;
                output += string.Format("-------------------------\n");
                output += string.Format("Distance of the route: {0} NM\n", solution.Value(distanceVar) / 100);
                output += string.Format("Gross pay of the route: ${0}\n", (double)routePay);
                output += "\n";
                output += "Copy to browser (logged in) to add to assignment:\n";
                output += route_assign;
                output += "\n\n\n";

                totalDistance += routeDistance;
                totalPay += routePay;
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
            this.model.AddDimension(countCall, 0, ASSIGNMENTCOUNTMAX, true, "assignment_visit_count");

            this.model.AddConstantDimension(1, this.model.Nodes() + 1, true, "node_count");

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

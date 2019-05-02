using System;
using System.Collections.Generic;
using System.Linq;
using Google.OrTools.ConstraintSolver;
using Google.Protobuf.WellKnownTypes;
using CsvHelper;
using System.IO;
using System.Net;
using SharpKml.Dom;
using SharpKml.Base;
using SharpKml.Engine;

namespace FSEcoRouteSolver
{
    class RouteProblem
    {
        class Node
        {
            public String Name { get; set; }
            public double Lat { get; set; }
            public double Lon { get; set; }
            public int Demand { get; set; }
            public int AssignmentId { get; set; }
            public int Pay { get; set; }
            public string Commodity { get; set; }
        }

        class JobRecord
        {
            public String FromIcao { get; set; }
            public String ToIcao { get; set; }
            public int Amount { get; set; }
            public decimal Pay { get; set; }
            public bool PtAssignment { get; set; }
            public string Commodity { get; set; }
            public int Id { get; set; }
        }

        class IcaoDataRecord
        {
            public String icao { get; set; }
            public double lat { get; set; }
            public double lon { get; set; }
        }

        string hub;
        readonly List<Node> nodes;
        readonly List<(int, int)> pdpPairs;

        class GreatCircleDistance
        {
            public long[,] cost_matrix;
            public GreatCircleDistance(RoutingIndexManager manager, long cpnm, List<Node> nodes)
            {
                this.manager = manager;
                cost_matrix = new long[nodes.Count, nodes.Count];
                for (var i = 0; i < nodes.Count; i++)
                {
                    for (var j = 0; j < nodes.Count; j++)
                    {
                        if (nodes[i].Name == nodes[j].Name)
                        {
                            cost_matrix[i, j] = 0;
                        }
                        else
                        {
                            cost_matrix[i, j] = (long)Haversine.calculate(nodes[i].Lat, nodes[i].Lon, nodes[j].Lat, nodes[j].Lon) * cpnm;
                        }
                    }
                }
            }

            public long Call(long from_idx, long to_idx)
            {
                var from = manager.IndexToNode(from_idx);
                var to = manager.IndexToNode(to_idx);
                return cost_matrix[from, to];
            }

            readonly RoutingIndexManager manager;
        }

        class TimeEvaluator
        {
            long[,] time_matrix;
            public TimeEvaluator(GreatCircleDistance distance, RoutingIndexManager manager, long spd, List<Node> nodes)
            {
                this.manager = manager;
                time_matrix = new long[nodes.Count, nodes.Count];
                for (var i = 0; i < nodes.Count; i++)
                {
                    for (var j = 0; j < nodes.Count; j++)
                    {
                        time_matrix[i, j] = distance.cost_matrix[i, j] / spd;
                    }
                }
            }

            public long Call(long from_idx, long to_idx)
            {
                var from = manager.IndexToNode(from_idx);
                var to = manager.IndexToNode(to_idx);
                return time_matrix[from, to];
            }

            readonly RoutingIndexManager manager;
        }

        public RouteProblem(string _hub, string api_key)
        {
            hub = _hub.ToUpper();
            var icaodata = new CsvReader(File.OpenText(@"./icaodata.csv"));
            var icaodata_records = icaodata.GetRecords<IcaoDataRecord>().ToDictionary(x => x.icao, x => x);
            var webClient = new WebClient();
            var to_jobs = new CsvReader(new StringReader(webClient.DownloadString(String.Format(@"http://server.fseconomy.net/data?userkey={0}&format=csv&query=icao&search=jobsto&icaos={1}", api_key, hub))));
            var from_jobs = new CsvReader(new StringReader(webClient.DownloadString(String.Format(@"http://server.fseconomy.net/data?userkey={0}&format=csv&query=icao&search=jobsfrom&icaos={1}", api_key, hub))));

            nodes = new List<Node>();
            pdpPairs = new List<(int, int)>();

            //"Depot" Node
            nodes.Add(new Node
            {
                Name = hub,
                Demand = 0,
                Lat = icaodata_records[hub].lat,
                Lon = icaodata_records[hub].lon,
                Pay = 0,
                Commodity = "Root"
            });

            foreach (var x in to_jobs.GetRecords<JobRecord>())
            {
                if (x.PtAssignment == true)
                {
                    var not_hub = x.FromIcao.ToUpper();

                    nodes.Add(new Node
                    {
                        Name = not_hub,
                        Demand = x.Amount,
                        Pay = 0,
                        Lat = icaodata_records[not_hub].lat,
                        Lon = icaodata_records[not_hub].lon,
                        AssignmentId = x.Id,
                        Commodity = x.Commodity
                    });

                    nodes.Add(new Node
                    {
                        Name = hub,
                        Demand = -x.Amount,
                        Pay = (int)Math.Round(x.Pay * 100),
                        Lat = icaodata_records[hub].lat,
                        Lon = icaodata_records[hub].lon,
                        AssignmentId = x.Id,
                        Commodity = x.Commodity
                    });

                    pdpPairs.Add((nodes.Count - 2, nodes.Count - 1));
                }
            }

            foreach (var x in from_jobs.GetRecords<JobRecord>())
            {
                if (x.PtAssignment == true)
                {
                    var not_hub = x.ToIcao.ToUpper();

                    nodes.Add(new Node
                    {
                        Name = hub,
                        Demand = x.Amount,
                        Pay = 0,
                        Lat = icaodata_records[hub].lat,
                        Lon = icaodata_records[hub].lon,
                        AssignmentId = x.Id,
                        Commodity = x.Commodity
                    });
                    nodes.Add(new Node
                    {
                        Name = not_hub,
                        Demand = -x.Amount,
                        Pay = (int)Math.Round(x.Pay * 100),
                        Lat = icaodata_records[not_hub].lat,
                        Lon = icaodata_records[not_hub].lon,
                        AssignmentId = x.Id,
                        Commodity = x.Commodity
                    });

                    pdpPairs.Add((nodes.Count - 2, nodes.Count - 1));
                }
            }
        }

        string PrintSolution(int numAircraft, int cpnm, in RoutingIndexManager manager, in RoutingModel model, in Assignment solution)
        {
            // Inspect solution.
            long totalDistance = 0;
            long totalPay = 0;
            string output = "";
            var distance = model.GetMutableDimension("distance");
            var bookingFee = model.GetMutableDimension("booking_fee");
            var assignmentCount = model.GetMutableDimension("assignment_count");
            Document doc = new Document
            {
                Id="Document"
            };
            for (int i = 0; i < numAircraft; ++i)
            {

                var routeAirport = new Folder
                {
                    Id = string.Format("airport-{0}", i),
                    Name = string.Format("Solution Airports {0}", i)
                };

                output += string.Format("Route for Aircraft {0}:\n", i);
                long routePay = 0;
                var index = model.Start(i);
                while (model.IsEnd(index) == false)
                {
                    var nodeIndex = manager.IndexToNode(index);
                    var node = nodes[nodeIndex];
                    var nodeId = node.Name + "-" + i;
                    var local_f = routeAirport.FindFeature(nodeId);
                    if(local_f == null)
                    {
                        var placemark = new Placemark
                        {
                            Id = nodeId,
                            Name = node.Name,
                            Geometry = new Point
                            {
                                Coordinate = new Vector(node.Lat, node.Lon)
                            }
                        };
                        routeAirport.AddFeature(placemark);
                    }
                    var bf = solution.Value(bookingFee.CumulVar(index));
                    var aob = solution.Value(assignmentCount.CumulVar(index));
                    if (node.Demand > 0)
                    {
                        output += string.Format("Pickup: {0}: {1}x {2}\n", node.Name, node.Demand, node.Commodity);
                    }
                    else
                    {
                       
                        output += string.Format("Deliver: {3} {1}x {2}, Pay: ${0}\n", node.Pay / 100, Math.Abs(node.Demand), node.Commodity, node.Name);
                        routePay += node.Pay / 100;
                    }
                    output += string.Format("BF: {0}, AoB: {1}\n", bf, aob);
                    var previousIndex = index;
                    index = solution.Value(model.NextVar(index));
                }
                var distanceVar = distance.CumulVar(index);
                long routeDistance = solution.Value(distanceVar) / 100;
                output += string.Format("\n\n\n");
                output += string.Format("Distance of the route: {0} NM\n", solution.Value(distanceVar) / 100);
                output += string.Format("Gross pay of the route: ${0}\n", ((double)routePay));
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
            output += string.Format("Total gross pay of all routes: ${0}\n", ((double)totalPay));

            //Console.WriteLine("{0}", solution.Value(bookingFee.CumulVar(model.End(0))));

            return output;
        }


        public string Solve(int numAircraft, int cpnm, int paxCapacity, int maxLength, int maxSolveSec, int maxTimeEnroute, int spd)
        {
            RoutingIndexManager manager = new RoutingIndexManager(nodes.Count, numAircraft, 0);
            RoutingModel model = new RoutingModel(manager);

            var distanceCostCall = new GreatCircleDistance(manager, cpnm, nodes);
            model.SetArcCostEvaluatorOfAllVehicles(model.RegisterTransitCallback(distanceCostCall.Call));

            var distanceCall = new GreatCircleDistance(manager, 100, nodes);
            model.AddDimension(model.RegisterTransitCallback(distanceCall.Call), 0, maxLength, true, "distance");
            var distance = model.GetMutableDimension("distance");

            var timeCall = new TimeEvaluator(distanceCall, manager, spd, nodes);
            model.AddDimension(model.RegisterTransitCallback(timeCall.Call), 0, maxTimeEnroute, true, "time");


            var bookingNeg = -paxCapacity;
            model.AddConstantDimensionWithSlack(bookingNeg, paxCapacity, 2*paxCapacity, true, "booking_fee");
            var bookingFee = model.GetMutableDimension("booking_fee");

            var totalPay = nodes.Select(n => n.Pay).Sum();

            model.AddConstantDimensionWithSlack(0, totalPay, totalPay, true, "bf_prime");
            var bfPrime = model.GetMutableDimension("bf_prime");

            int assignmentCallBackIndex = model.RegisterUnaryTransitCallback(
                (long fromIndex) =>
                {
                    var fromNode = manager.IndexToNode(fromIndex);
                    return Math.Sign(nodes[fromNode].Demand);
                }
                );
            model.AddDimension(
              assignmentCallBackIndex, 0,  // null capacity slack
              paxCapacity,   // vehicle maximum capacities
              true,                      // start cumul to zero
              "assignment_count");
            var assignmentCount = model.GetMutableDimension("assignment_count");

            int demandCallbackIndex = model.RegisterUnaryTransitCallback(
                (long fromIndex) => {
                    // Convert from routing variable Index to demand NodeIndex.
                    var fromNode = manager.IndexToNode(fromIndex);
                    return nodes[fromNode].Demand;
                }
            );

            model.AddDimension(
              demandCallbackIndex, 0,  // null capacity slack
              paxCapacity,   // vehicle maximum capacities
              true,                      // start cumul to zero
              "capacity");

            var capacity = model.GetMutableDimension("capacity");

            var solver = model.solver();
            foreach (var pair in pdpPairs)
            {
                (var pickup, var delivery) = pair;
                var pickup_idx = manager.NodeToIndex(pickup);
                var delivery_idx = manager.NodeToIndex(delivery);

                model.AddPickupAndDelivery(pickup_idx, delivery_idx);
                solver.Add(distance.CumulVar(pickup_idx) <= distance.CumulVar(delivery_idx));
                solver.Add(model.VehicleVar(pickup_idx) == model.VehicleVar(delivery_idx));

                solver.Add(model.ActiveVar(pickup_idx) == model.ActiveVar(delivery_idx));

                model.AddDisjunction(new long[] { pickup_idx }, nodes[pickup].Pay);
                model.AddDisjunction(new long[] { delivery_idx }, nodes[delivery].Pay);
                //model.AddDisjunction(new long[] { pickup_idx, delivery_idx }, nodes[delivery].Pay, 2);
            }


            //Booking Fee. Does not Implement the 5 assignment floor.
            //for (int i = 0; i < nodes.Count; i++)
            //{
            //    var ii = manager.NodeToIndex(i);

            //    solver.Add(bookingFee.CumulVar(ii) >= assignmentCount.CumulVar(ii));

            //    var notNextIsZero = (assignmentCount.TransitVar(ii) + assignmentCount.CumulVar(ii)) != 0;
            //    var delBookingFee = bookingFee.SlackVar(ii) + bookingNeg;

            //    solver.Add((notNextIsZero * delBookingFee) >= 0);
            //}

            for (int i = 0; i < nodes.Count; i++)
            {
                var ii = manager.NodeToIndex(i);

                var acGtFive = assignmentCount.CumulVar(ii) > 5;

                solver.Add(acGtFive * bookingFee.CumulVar(ii) >= acGtFive * assignmentCount.CumulVar(ii));

                var notNextIsZero = (assignmentCount.TransitVar(ii) + assignmentCount.CumulVar(ii)) != 0;
                var delBookingFee = bookingFee.SlackVar(ii) + bookingNeg;

                solver.Add((notNextIsZero * delBookingFee) >= 0);
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                var ii = manager.NodeToIndex(i);

                var math = solver.MakeDiv(nodes[i].Pay * bookingFee.CumulVar(ii), 100);

                var prime = bfPrime.SlackVar(ii) == (math * model.ActiveVar(ii));
                solver.Add(prime);
            }

            bfPrime.SetSpanCostCoefficientForAllVehicles(1);

            var routingParameters = operations_research_constraint_solver.DefaultRoutingSearchParameters();
            routingParameters.LocalSearchMetaheuristic = LocalSearchMetaheuristic.Types.Value.GuidedLocalSearch;
            routingParameters.FirstSolutionStrategy = FirstSolutionStrategy.Types.Value.LocalCheapestInsertion;
            routingParameters.LogSearch = true;
            routingParameters.TimeLimit = new Duration { Seconds = maxSolveSec };

            var solution = model.SolveWithParameters(routingParameters);

            return PrintSolution(numAircraft, cpnm, manager, model, solution);

        }
    }


}

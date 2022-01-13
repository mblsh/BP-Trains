using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPTrains
{
    class Program
    {
        static MailTrainsSystem ParseInput(string filename)
        {
            List<Station> stations = new List<Station>();
            List<Route> routes = new List<Route>();
            List<Train> trains = new List<Train>();
            List<Package> deliveries = new List<Package>(); 

            var input = File.ReadAllLines(filename);
            var cleanedInput = input.Where(l=>!l.StartsWith("//") && !string.IsNullOrEmpty(l.Trim())).ToList();
            var currentLine = 0;

            try
            {
                var stationsCount = Int32.Parse(cleanedInput[currentLine].Split(new[] { "//" }, StringSplitOptions.None)[0].Trim());
                currentLine++;
                for (int s = 0; s < stationsCount; s++)
                {
                    stations.Add(new Station() { Name = cleanedInput[currentLine + s].Split(new[] { "//" }, StringSplitOptions.None)[0].Trim() });
                }
                currentLine += stationsCount;

                var routesCount = Int32.Parse(cleanedInput[currentLine].Split(new[] { "//" }, StringSplitOptions.None)[0].Trim());
                currentLine++;
                for (int r = 0; r < routesCount; r++)
                {
                    var routeStr = cleanedInput[currentLine + r].Split(new[] { "//" }, StringSplitOptions.None)[0].Trim().Split(',');
                    var startStation = stations.First(st => st.Name == routeStr[1]);
                    var endStation = stations.First(st => st.Name == routeStr[2]);
                    if (startStation == endStation) // remove edge case
                        continue;

                    var route = new Route
                        {
                            Name = routeStr[0],
                            Start = startStation,
                            Destination = endStation,
                            TravelTime = int.Parse(routeStr[3])
                        };

                    routes.Add(route);

                    startStation.Connections.Add(route);
                    
                    var reverseRoute = new Route
                    {
                        Name = routeStr[0],
                        Destination = startStation,
                        Start = endStation,
                        TravelTime = int.Parse(routeStr[3])
                    };
                    endStation.Connections.Add(reverseRoute); 
                    routes.Add(reverseRoute);
                    // task says "A train may only traverse an edge if it is at one node
                    // or the other of that edge", meaning that routes are bi-directional
                }
                currentLine += routesCount;

                var deliveriesCount = Int32.Parse(cleanedInput[currentLine].Split(new[] { "//" }, StringSplitOptions.None)[0].Trim());
                currentLine++;
                for (int d = 0; d < deliveriesCount; d++)
                {
                    var packageStr = cleanedInput[currentLine + d].Split(new[] { "//" }, StringSplitOptions.None)[0].Trim().Split(',');
                    var pickUpStation = stations.First(st => st.Name == packageStr[1]);
                    var dropOffStation = stations.First(st => st.Name == packageStr[2]);
                    if (pickUpStation == dropOffStation) // no need to move this package
                        continue;

                    deliveries.Add(new Package
                    {
                        Name = packageStr[0],
                        PickUp = pickUpStation,
                        DropOff = dropOffStation,
                        Weight = int.Parse(packageStr[3])
                    });
                }
                currentLine += deliveriesCount;

                var trainsCount = Int32.Parse(cleanedInput[currentLine].Split(new[] { "//" }, StringSplitOptions.None)[0].Trim());
                currentLine++;
                for (int t = 0; t < trainsCount; t++)
                {
                    var trainStr = cleanedInput[currentLine + t].Split(new[] { "//" }, StringSplitOptions.None)[0].Trim().Split(',');
                    trains.Add(new Train
                    {
                        Name = trainStr[0],
                        CurrentStation = stations.First(st => st.Name == trainStr[1]),
                        Capacity = int.Parse(trainStr[2])
                    });
                }

                if (trains.Max(t => t.Capacity) < deliveries.Max(d => d.Weight))
                {
                    Console.WriteLine($"Bad input: train capacity is not enough");
                    return null;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Bad input: {e.Message}");
                return null;
            }

            return new MailTrainsSystem(stations, routes, trains, deliveries);
        }

        static void PrintOutput(List<Move> moves)
        {
            foreach (var move in moves)
            {
                var load = String.Join(",", move.PickUps.Select(p => p.Name));
                var drop = String.Join(",", move.DropOffs.Select(p => p.Name));
                var moving = "parked";
                var arr = "";
                if (move.Route != null)
                {
                    moving = $"moving {move.Route.Start.Name}->{move.Route.Destination.Name}:{move.Route.Name}";
                    arr = $" arr {move.StartTime + move.Route.TravelTime}";
                }

                Console.WriteLine($"@{move.StartTime}, n = {move.StartStation.Name}, q = {move.Train.Name}, load= {{ {load} }}, drop= {{ {drop} }}, {moving}{arr}");
            }
        }

        static void Main(string[] args)
        {
            var filename = "input5.txt";
            if (args.Length > 0 && !string.IsNullOrEmpty(args[0]))
                filename = args[0];

            var mtsOneByOne = ParseInput(filename);
            var moves = mtsOneByOne.SolveViaSingleDeliveries();
            Console.WriteLine($"Deliver one-by-one ({moves.Count} moves)");
            PrintOutput(moves);

            // solver modifies the MTS state, so need to re-create to  try out another solution
            var mtsWithPickups = ParseInput(filename); 
            moves = mtsWithPickups.SolveWithPickUpsAlongRoute();
            Console.WriteLine($"Deliver with pickups ({moves.Count} moves)");
            PrintOutput(moves);

            var mtsWithGreedyTrains = ParseInput(filename);
            moves = mtsWithGreedyTrains.SolveWithGreedyTrains();
            Console.WriteLine($"Deliver with greedy trains ({moves.Count} moves)");
            PrintOutput(moves);

            mtsWithPickups.SolveBetter(); // <- ideas for improvement inside
        }
    }
}

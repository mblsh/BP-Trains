using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace BPTrains
{
    public class MailTrainsSystem
    {
        private List<Station> _stations;
        private List<Route> _routes;
        private List<Train> _trains;
        private List<Package> _deliveries;

        public MailTrainsSystem(List<Station> stations,
            List<Route> routes,
            List<Train> trains,
            List<Package> deliveries)
        {
            _stations = stations;
            _routes = routes;
            _trains = trains;
            _deliveries = deliveries;
        }

        private List<Route> GetFastestPath(Station start, Station end)
        {
            var retval = new List<Route>();

            if (start == end)
                return retval;

            var stations = new HashSet<Station>();
            var times = new Dictionary<Station, int>();
            var viaRoutes = new Dictionary<Station, Route>();
            foreach (var station in _stations)
            {
                stations.Add(station);
                times.Add(station, station == start ? 0 : int.MaxValue);
                viaRoutes.Add(station, null);
            }

            while (stations.Count > 0)
            {
                var removedStation = times.OrderBy(kvp => kvp.Value).First().Key;
                stations.Remove(removedStation);
                var removedTime = times[removedStation];
                times.Remove(removedStation);


                if (removedStation == end)
                    break;

                foreach (var route in removedStation.Connections)
                {
                    if (!stations.Contains(route.Destination))
                        continue;

                    var connection = route.Destination;
                    var t = removedTime + route.TravelTime;
                    if (t < times[connection])
                    {
                        times[connection] = t;
                        viaRoutes[connection] = route;
                    }
                }
            }

            if (viaRoutes[end] == null)
                return null; // route not found

            // backtrack the routes and return moveset
            var currentBacktrackedNode = end;
            while (currentBacktrackedNode != start)
            {
                var r = viaRoutes[currentBacktrackedNode];
                if (r == null)
                    return null; // route not found

                retval.Insert(0, r);
                currentBacktrackedNode = r.Start;
            }

            return retval;
        }

        public List<Move> SolveViaSingleDeliveries()
        {
            var moves = new List<Move>();
            // for each package find a train with enough capacity which can arrive fastest at pick-up point
            // pick the one with with least travel time to a package
            // plan route from pick up to drop off, add to list of moves, remove the package from deliveries list
            // mark down time spent en route, to give a 'free' train priority for the next delivery
            while (_deliveries.Count > 0)
            {
                var chosenPackage = _deliveries[0];
                Train chosenTrain = null;
                var routeToPickUpPoint = new List<Route>();
                var currentBestTimeToPackage = int.MaxValue;
                var minExcessTime =
                    _trains.Min(t =>
                        t.TimeSpentEnRoute); // will be used to adjust 'real' time to package across several trains

                // find the closest pair of train and package
                // for every package
                foreach (var package in _deliveries)
                {
                    // ..and every train with enough capacity
                    foreach (var train in _trains.Where(t => t.Capacity >= package.Weight))
                    {
                        // get the fastest route from current train position to the pick up point
                        var fastestRouteToPickUpPoint = GetFastestPath(train.CurrentStation, package.PickUp);
                        // ...provided the route exists (train can be on a different rail network)
                        if (fastestRouteToPickUpPoint == null)
                            continue;

                        // consider the time this particular train spent en route already
                        // to allow for parallel deliveries
                        var timeToPickUp = fastestRouteToPickUpPoint.Sum(r => r.TravelTime);
                        var totalTime =
                            train.TimeSpentEnRoute + timeToPickUp -
                            minExcessTime; //despite still being 'en route' a busy train can be closer to a package than a free one
                        if (totalTime < currentBestTimeToPackage)
                        {
                            currentBestTimeToPackage = totalTime;
                            chosenPackage = package;
                            chosenTrain = train;
                            routeToPickUpPoint = fastestRouteToPickUpPoint;
                        }
                    }
                }

                if (chosenTrain == null)
                    throw new Exception("Unreachable package");


                // add route to chosen package to the list of moves
                var ct = chosenTrain.TimeSpentEnRoute;
                foreach (var route in routeToPickUpPoint)
                {
                    moves.Add(new Move()
                        {Route = route, StartTime = ct, StartStation = route.Start, Train = chosenTrain});
                    ct += route.TravelTime;
                }

                var routeToDropOff = GetFastestPath(chosenPackage.PickUp, chosenPackage.DropOff);
                if (routeToDropOff == null)
                    throw new Exception($"Unreachable package destination for {chosenPackage.Name}");

                var m = new Move()
                {
                    Route = routeToDropOff.First(),
                    StartStation = routeToDropOff.First().Start,
                    StartTime = ct,
                    Train = chosenTrain
                };
                m.PickUps.Add(chosenPackage);
                moves.Add(m);
                ct += routeToDropOff.First().TravelTime;

                for (int i = 1; i < routeToDropOff.Count; i++)
                {
                    m = new Move()
                    {
                        Route = routeToDropOff[i],
                        StartStation = routeToDropOff[i].Start,
                        StartTime = ct,
                        Train = chosenTrain
                    };
                    moves.Add(m);
                    ct += routeToDropOff[i].TravelTime;
                }

                m =
                    new
                        Move() // null move to drop off package; can be combined with the start of the next move, but kept it split for hte purposes of this exercise
                        {
                            Route = null,
                            StartStation = routeToDropOff.Last().Destination,
                            StartTime = ct,
                            Train = chosenTrain
                        };
                m.DropOffs.Add(chosenPackage);
                moves.Add(m);

                chosenTrain.TimeSpentEnRoute = ct;
                chosenTrain.CurrentStation = m.StartStation;

                // remove the package from the list of remaining deliveries and repeat
                _deliveries.Remove(chosenPackage);
            }

            // combine null drop moves with subsequent pickups, if any
            // all the below is not optimal on large number of moves, 
            // but it's the easiest option for the purposes of this exercise
            foreach (var dropMove in moves.Where(m => m.Route == null && m.DropOffs.Count > 0 && m.PickUps.Count == 0)
                .ToList())
            {
                var combine = moves.FirstOrDefault(m => m.StartTime == dropMove.StartTime
                                                        && m.StartStation == dropMove.StartStation
                                                        && m.Train == dropMove.Train
                                                        && m.Route != null);
                if (combine != null)
                {
                    moves.Remove(combine);
                    dropMove.PickUps.AddRange(combine.PickUps);
                    dropMove.Route = combine.Route;
                }
            }

            return moves.OrderBy(m => m.StartTime).ToList();
        }

        public List<Move> SolveWithPickUpsAlongRoute()
        {
            var moves = new List<Move>();
            // for each package find a train with enough capacity which can arrive fastest at pick-up point
            // pick the one with with least travel time to a package
            // plan route from pick up to drop off, add to list of moves, remove the package from deliveries list
            // mark down time spent en route, to give a 'free' train priority for the next delivery
            while (_deliveries.Count > 0)
            {
                var chosenPackage = _deliveries[0];
                Train chosenTrain = null;
                var routeToPickUpPoint = new List<Route>();
                var currentBestTimeToPackage = int.MaxValue;
                var minExcessTime =
                    _trains.Min(t =>
                        t.TimeSpentEnRoute); // will be used to adjust 'real' time to package across several trains

                // find the closest pair of train and package
                // for every package
                foreach (var package in _deliveries)
                {
                    // ..and every train with enough capacity
                    foreach (var train in _trains.Where(t => t.Capacity >= package.Weight))
                    {
                        // get the fastest route from current train position to the pick up point
                        var fastestRouteToPickUpPoint = GetFastestPath(train.CurrentStation, package.PickUp);
                        // ...provided the route exists (train can be on a different rail network)
                        if (fastestRouteToPickUpPoint == null)
                            continue;

                        // consider the time this particular train spent en route already
                        // to allow for parallel deliveries
                        var timeToPickUp = fastestRouteToPickUpPoint.Sum(r => r.TravelTime);
                        var totalTime =
                            train.TimeSpentEnRoute + timeToPickUp -
                            minExcessTime; //despite still being 'en route' a busy train can be closer to a package than a free one
                        if (totalTime < currentBestTimeToPackage)
                        {
                            currentBestTimeToPackage = totalTime;
                            chosenPackage = package;
                            chosenTrain = train;
                            routeToPickUpPoint = fastestRouteToPickUpPoint;
                        }
                    }
                }

                if (chosenTrain == null)
                    throw new Exception("Unreachable package");


                // add route to chosen package to the list of moves
                var ct = chosenTrain.TimeSpentEnRoute;
                foreach (var route in routeToPickUpPoint)
                {
                    moves.Add(new Move()
                        {Route = route, StartTime = ct, StartStation = route.Start, Train = chosenTrain});
                    ct += route.TravelTime;
                }

                var routeToDropOff = GetFastestPath(chosenPackage.PickUp, chosenPackage.DropOff);
                if (routeToDropOff == null)
                    throw new Exception($"Unreachable package destination for {chosenPackage.Name}");

                ///// START CHANGE FROM PREVIOUS 
                var firstStop = true;
                while (routeToDropOff.Count > 0)
                {
                    var nextRoute = routeToDropOff[0];
                    var m = new Move()
                    {
                        Route = nextRoute,
                        StartStation = nextRoute.Start,
                        StartTime = ct,
                        Train = chosenTrain
                    };

                    // drop off packages picked up along the way
                    var packages2dropoff = chosenTrain.Packages.Where(p => p.DropOff == nextRoute.Start).ToList();
                    foreach (var p2d in packages2dropoff)
                    {
                        m.DropOffs.Add(p2d);
                        chosenTrain.Packages.Remove(p2d);
                    }

                    // pick up first and/or any package which happens to be along the way
                    Package package2pickup;
                    if (firstStop)
                    {
                        package2pickup = chosenPackage;
                        firstStop = false;
                    }
                    else
                    {
                        package2pickup = _deliveries.FirstOrDefault(d => d.PickUp == nextRoute.Start
                                                                         && routeToDropOff.Select(r => r.Destination)
                                                                             .Contains(d.DropOff)
                                                                         && d.Weight <= chosenTrain.FreeCapacity);
                    }

                    while (package2pickup != null)
                    {
                        m.PickUps.Add(package2pickup);
                        _deliveries.Remove(package2pickup);
                        chosenTrain.Packages.Add(package2pickup);

                        package2pickup = _deliveries.FirstOrDefault(d => d.PickUp == nextRoute.Start
                                                                         && routeToDropOff.Select(r => r.Destination)
                                                                             .Contains(d.DropOff)
                                                                         && d.Weight <= chosenTrain.FreeCapacity);
                    }

                    moves.Add(m);
                    ct += nextRoute.TravelTime;
                    routeToDropOff.RemoveAt(0);
                }

                // drop off everything at final destination and stay 
                var lastm = new Move() 
                            {
                                Route = null,
                                StartStation = moves.Last().Route.Destination,
                                StartTime = ct,
                                Train = chosenTrain
                            };
                lastm.DropOffs.AddRange(chosenTrain.Packages);
                chosenTrain.Packages.Clear();
                moves.Add(lastm);
                ///// END CHANGE FROM PREVIOUS 

                chosenTrain.TimeSpentEnRoute = ct;
                chosenTrain.CurrentStation = lastm.StartStation;
            }

            // combine null drop moves with subsequent pickups, if any
            // all the below is not optimal on large number of moves, 
            // but it's the easiest option for the purposes of this exercise
            foreach (var dropMove in moves.Where(m => m.Route == null && m.DropOffs.Count > 0 && m.PickUps.Count == 0)
                .ToList())
            {
                var combine = moves.FirstOrDefault(m => m.StartTime == dropMove.StartTime
                                                        && m.StartStation == dropMove.StartStation
                                                        && m.Train == dropMove.Train
                                                        && m.Route != null);
                if (combine != null)
                {
                    moves.Remove(combine);
                    dropMove.PickUps.AddRange(combine.PickUps);
                    dropMove.Route = combine.Route;
                }
            }

            return moves.OrderBy(m => m.StartTime).ToList();
        }

        public List<Move> SolveWithGreedyTrains()
        {
            var moves = new List<Move>();

            int currentTime = 0;

            // iterate over time and consider system state at every timeframe
            while (true)
            {
                // look for an idle trains (idle == not en route to next destination and still have valid targets)
                foreach (var train in _trains.Where(t => t.TimeSpentEnRoute <= currentTime && !t.Parked))
                {
                    // plan the next move
                    var nextMove = new Move() { StartStation = train.CurrentStation, StartTime = currentTime, Train = train };

                    //see if it need to do a drop off or can pick something up at current station
                    var packages2dropoff = train.Packages.Where(p => p.DropOff == train.CurrentStation).ToList();
                    foreach (var p2d in packages2dropoff)
                    {
                        nextMove.DropOffs.Add(p2d);
                        train.Packages.Remove(p2d);
                    }

                    Package package2pickup = _deliveries.FirstOrDefault(d => d.PickUp == train.CurrentStation
                                                                         && d.Weight <= train.FreeCapacity);
                    while (package2pickup != null)
                    {
                        nextMove.PickUps.Add(package2pickup);
                        _deliveries.Remove(package2pickup);
                        train.Packages.Add(package2pickup);

                        package2pickup = _deliveries.FirstOrDefault(d => d.PickUp == train.CurrentStation
                                                                         && d.Weight <= train.FreeCapacity);
                    }

                    //plan the route, choosing the nearest valid pick up or drop off
                    var potentialTargets = _deliveries.Where(d=> d.Weight <= train.FreeCapacity)
                                                                .Select(d => d.PickUp)
                                                                .Union(train.Packages.Select(p => p.DropOff));

                    if (!potentialTargets.Any())
                    {
                        // train has dropped everything off and there are no more suitable deliveries left
                        train.Parked = true;
                        nextMove.Route = null;
                        moves.Add(nextMove);
                        continue;
                    }

                    Station closestNextStation = null;
                    Route nextRoute = null;
                    var fastestTime = int.MaxValue;
                    // consider potential next stations one by one and find the closest one
                    foreach (var nextStation in potentialTargets)
                    {
                        var fastestRoute = GetFastestPath(train.CurrentStation, nextStation);
                        if (fastestRoute == null)
                            continue; // destination unreachable (package on different sub-network)
                        var routeTime = fastestRoute.Sum(r => r.TravelTime);
                        if (routeTime < fastestTime)
                        {
                            fastestTime = routeTime;
                            nextRoute = fastestRoute.First();
                            closestNextStation = nextRoute.Destination;
                        }
                    }

                    if (closestNextStation == null) 
                    {
                        // unsolvable - one or more packages cannot be delivered
                        train.Parked = true;
                        nextMove.Route = null;
                        moves.Add(nextMove);
                        continue;
                    }

                    // start the train towards the chosen station
                    train.CurrentStation = closestNextStation;
                    train.TimeSpentEnRoute += nextRoute.TravelTime;
                    nextMove.Route = nextRoute;
                    moves.Add(nextMove);
                }

                currentTime++;

                if (_trains.All(t => t.Parked))
                    break;
            }

            return moves.OrderBy(m => m.StartTime).ToList();
        }


        // TODO:
        public void SolveBetter()
        {
            // Ideas
            //
            // while looking for a fastest route, reduce the weight of edges where vertices contain packages
            // to pick up, in order to nudge the train routes towards making pick ups along the way
            //
            // reduce the graph by removing stations with no packages along unique paths 
            // 
            // break up the rail network into components served by dedicated trains to promote route locality?
            // train can carry a set of edge weight reduction coefficients to tilt the pathfinding towards a particular
            // portion of the network
        }
    }
}



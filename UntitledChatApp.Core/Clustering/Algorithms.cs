﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UntitledChatApp.Core.Clustering
{
    public class ClusterItem<T>
    {
        public T Value { get; set; }
        public CartesianCoordinates Mean { get; set; }

        public override string ToString()
        {
            return Value.ToString();
        }
    }

    public class Cluster<T>
    {
        public List<ClusterItem<T>> Values { get; set; }
        public CartesianCoordinates Mean { get; set; }

        public Cluster()
        {
            Values = new List<ClusterItem<T>>();
        }

        public void RecalculateMean()
        {
            Mean = Values.Select(o => o.Mean).ToArray().GetGeographicMidpoint().midpoint;
        }
    }

    public class SortedCluster<T> : Cluster<T>
    {
        public SortedCluster(Cluster<T> cluster)
        {
            this.Mean = cluster.Mean;
            this.Values = cluster.Values
                .Select(o =>
                    new
                    {
                        item = o,
                        distanceFromCluster = GPSMath.DistanceSquared(o.Mean, Mean),
                    })
                .OrderBy(o => o.distanceFromCluster)
                .Select(o => o.item)
                .ToList();
        }
    }

    public static class ClusterExtensions
    {
        public static Cluster<T> NearestTo<T>(this IEnumerable<Cluster<T>> clusters, CartesianCoordinates point)
        {
            var nearestDistance = double.MaxValue;
            Cluster<T> nearest = null;

            foreach (var cluster in clusters)
            {
                var distance = GPSMath.DistanceSquared(cluster.Mean, point);
                if (distance < nearestDistance)
                {
                    nearest = cluster;
                    nearestDistance = distance;
                }
            }
            return nearest;
        }

        public static void MoveOverflowTo<T>(this Cluster<T> source, Cluster<T> target, int balancedNum)
        {
            var sortedSource = new SortedCluster<T>(source);
            var overflow = sortedSource.Values.Skip(balancedNum);
            foreach (var item in overflow)
            {
                source.Values.Remove(item);
            }
            target.Values.AddRange(overflow);
        }
    }

    public class Algorithms
    {
        public IEnumerable<Cluster<T>> ComputeKMeans<T>(
            IEnumerable<ClusterItem<T>> source, 
            int numberOfClusters,
            int maxIterations)
        {
            var clusters = ComputeInitialClustering(source, numberOfClusters);
            int currentIteration = 1;

            while (currentIteration++ < maxIterations)
            {
                ReCluster(clusters, source);
            }

            return clusters;
        }

        void ReCluster<T>(IEnumerable<Cluster<T>> clusters, IEnumerable<ClusterItem<T>> source)
        {
            foreach (var cluster in clusters)
                cluster.Values.Clear();

            foreach (var item in source)
            {
                var closestCluster = clusters.NearestTo(item.Mean);
                closestCluster.Values.Add(item);
            }

            foreach (var cluster in clusters)
            {
                cluster.RecalculateMean();
            }
        }

        IEnumerable<Cluster<T>> ComputeInitialClustering<T>(IEnumerable<ClusterItem<T>> source, int numClusters)
        {
            var clusters = Enumerable.Range(0, numClusters).Select(o => new Cluster<T>()).ToList();
            int index = 0;
            foreach (var item in source)
            {
                var bucket = index++ % numClusters;
                clusters[bucket].Values.Add(item);
            }
            foreach (var cluster in clusters)
            {
                cluster.RecalculateMean();
            }
            return clusters;
        }
    }
}

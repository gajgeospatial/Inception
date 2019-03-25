﻿using System;
using UnityEngine;
using Cognitics.CoordinateSystems;

namespace Cognitics.UnityCDB
{

    public class SurfaceCollider : MonoBehaviour
    {
        public Database Database = null;
        public double minCameraElevation = 0.0f;

        private Vector3[] verticesBelowCamera = new Vector3[4];
        private int[] indicesBelowCamera = new int[4];

        void Update()
        {
            if (Database == null)
                return;
            GetTerrainElevation();
        }

        private void GetTerrainElevation()
        {
            Tile tile = GetTileBelowCamera();
            if (tile == null || tile.IsLoaded == false)
                return;

            CartesianBounds cartesianBounds = tile.GeographicBounds.TransformedWith(Database.Projection);

            double spcX = (cartesianBounds.MaximumCoordinates.X - cartesianBounds.MinimumCoordinates.X) / tile.MeshDimension;
            double spcZ = (cartesianBounds.MaximumCoordinates.Y - cartesianBounds.MinimumCoordinates.Y) / tile.MeshDimension;
            double orgX = cartesianBounds.MinimumCoordinates.X;
            double orgZ = cartesianBounds.MinimumCoordinates.Y;

            float xComponent = transform.position.x - (float)orgX;
            float zComponent = transform.position.z - (float)orgZ;

            int xIndex = Math.Min(tile.MeshDimension - 2, (int)Math.Floor(xComponent / spcX));
            int zIndex = Math.Min(tile.MeshDimension - 2, (int)Math.Floor(zComponent / spcZ));

            indicesBelowCamera[0] = zIndex * tile.MeshDimension + xIndex;
            indicesBelowCamera[1] = indicesBelowCamera[0] + 1;
            indicesBelowCamera[2] = indicesBelowCamera[0] + tile.MeshDimension;
            indicesBelowCamera[3] = indicesBelowCamera[0] + tile.MeshDimension + 1;
            for (int i = 0; i < indicesBelowCamera.Length; ++i)
                verticesBelowCamera[i] = tile.vertices[indicesBelowCamera[i]];

            SetMinElevationWithTriangle(transform.position, verticesBelowCamera[0], verticesBelowCamera[2], verticesBelowCamera[3]);
            SetMinElevationWithTriangle(transform.position, verticesBelowCamera[0], verticesBelowCamera[1], verticesBelowCamera[3]);
        }


        void SetMinElevationWithTriangle(Vector3 position, Vector3 a, Vector3 b, Vector3 c)
        {
            if (InterpolatePointInPlane(ref position, a, b, c))
            {
                float minElevation = Math.Min(Math.Min(a.y, b.y), c.y);
                float maxElevation = Math.Max(Math.Max(a.y, b.y), c.y);
                minCameraElevation = maxElevation;
                if ((position.y >= minElevation) && (position.y <= maxElevation))
                    minCameraElevation = position.y;
            }
        }

        public Tile GetTileBelowCamera()
        {
            var activeTiles = Database.ActiveTiles();
            var camera = new CartesianCoordinates(transform.position.x, transform.position.z);
            for (int i = 0; i < activeTiles.Count; ++i)
            {
                CartesianBounds cartesianBounds = activeTiles[i].GeographicBounds.TransformedWith(Database.Projection);
                if (cartesianBounds.MaximumCoordinates.X > camera.X
                    && cartesianBounds.MaximumCoordinates.Y > camera.Y
                    && cartesianBounds.MinimumCoordinates.X < camera.X
                    && cartesianBounds.MinimumCoordinates.Y < camera.Y)
                {
                    return activeTiles[i];
                }
            }
            return null;
        }

        /* Since Unity's Normalize doesn't normalize */
        public static void Normalize(ref Vector3 v)
        {
            double len = Math.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);

            if (len > 0 && len <= float.MaxValue)
            {
                double inv = 1.0f / len;
                v.x *= (float)inv;
                v.y *= (float)inv;
                v.z *= (float)inv;
            }
        }

        public static bool InterpolatePointInPlane(ref Vector3 point, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            double EPSILON = 0.000001;

            // if p2 == p1

            if ((Math.Abs(p2.x - p1.x) < EPSILON) && (Math.Abs(p2.z - p1.z) < EPSILON))
            {
                // if p3 == p1, they are all equal
                if ((Math.Abs(p3.x - p1.x) < EPSILON) && (Math.Abs(p3.z - p1.z) < EPSILON))
                {
                    // so move both along the different axes
                    p2.x = p2.x + 1;
                    p3.z = p3.z + 1;
                }

                else
                {
                    // otherwise, move p2 along the perpendicular of the normal of p3-p1
                    Vector3 p = p3 - p1;

                    Normalize(ref p);
                    p2.x = p2.x - p.z;
                    p2.z = p2.z + p.x;
                }
            }
            // if p3 == p1 or p3 == p2 (we've already handled the p1 and p2 relationship)
            if (((Math.Abs(p3.x - p1.x) < EPSILON) && (Math.Abs(p3.z - p1.z) < EPSILON))
                || ((Math.Abs(p3.x - p2.x) < EPSILON) && (Math.Abs(p3.z - p2.z) < EPSILON)))
            {
                // we've already handled all three equal
                // move p3 along the perpendicular of the normal of p2-p1
                Vector3 p = p2 - p1;
                Normalize(ref p);
                p3.x = p3.x - p.z;
                p3.z = p3.z + p.x;
            }

            Vector3 P = point - p1;
            Vector3 U = p2 - p1;
            Vector3 V = p3 - p1;

            float denom = V.x * U.z - V.z * U.x;
            if (denom == 0.0f)
                return false; // Returns 0 if vectors are co-linear
            float v = (P.x * U.z - P.z * U.z) / denom;
            float u = (P.z * V.x - P.x * V.z) / denom;
            point.y = (p1.y + (u * U.y + (v * V.y)));
            return !float.IsNaN(point.y);
        }

        public void TerrainElevationGetter()
        {
            GetTerrainElevation();
        }

    }

}

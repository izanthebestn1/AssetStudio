using AssetStudio.FbxInterop;
using AssetStudio.PInvoke;
using System;
using System.IO;

namespace AssetStudio
{
    public static partial class Fbx
    {
        private static readonly bool nativeAvailable = TryPreloadNative();

        public static bool IsNativeAvailable => nativeAvailable;

        private static bool TryPreloadNative()
        {
            try
            {
                DllLoader.PreloadDll(FbxDll.DllName);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Warning($"FBX native runtime is unavailable: {ex.Message}");
                return false;
            }
        }

        public static Vector3 QuaternionToEuler(Quaternion q)
        {
            if (nativeAvailable)
            {
                try
                {
                    AsUtilQuaternionToEuler(q.X, q.Y, q.Z, q.W, out var x, out var y, out var z);
                    return new Vector3(x, y, z);
                }
                catch (Exception ex) when (ex is DllNotFoundException || ex is EntryPointNotFoundException || ex is BadImageFormatException || ex is TypeInitializationException)
                {
                    Logger.Warning($"FBX quaternion conversion fallback in use: {ex.Message}");
                }
            }

            return QuaternionToEulerManaged(q);
        }

        public static Quaternion EulerToQuaternion(Vector3 v)
        {
            if (nativeAvailable)
            {
                try
                {
                    AsUtilEulerToQuaternion(v.X, v.Y, v.Z, out var x, out var y, out var z, out var w);
                    return new Quaternion(x, y, z, w);
                }
                catch (Exception ex) when (ex is DllNotFoundException || ex is EntryPointNotFoundException || ex is BadImageFormatException || ex is TypeInitializationException)
                {
                    Logger.Warning($"FBX euler conversion fallback in use: {ex.Message}");
                }
            }

            return EulerToQuaternionManaged(v);
        }

        private static Vector3 QuaternionToEulerManaged(Quaternion q)
        {
            // Matches common XYZ intrinsic decomposition used by most DCC tooling.
            var sinrCosp = 2f * (q.W * q.X + q.Y * q.Z);
            var cosrCosp = 1f - 2f * (q.X * q.X + q.Y * q.Y);
            var x = (float)Math.Atan2(sinrCosp, cosrCosp);

            var sinp = 2f * (q.W * q.Y - q.Z * q.X);
            float y;
            if (Math.Abs(sinp) >= 1f)
            {
                y = (float)Math.CopySign(Math.PI / 2d, sinp);
            }
            else
            {
                y = (float)Math.Asin(sinp);
            }

            var sinyCosp = 2f * (q.W * q.Z + q.X * q.Y);
            var cosyCosp = 1f - 2f * (q.Y * q.Y + q.Z * q.Z);
            var z = (float)Math.Atan2(sinyCosp, cosyCosp);

            return new Vector3(x, y, z);
        }

        private static Quaternion EulerToQuaternionManaged(Vector3 v)
        {
            var halfX = v.X * 0.5f;
            var halfY = v.Y * 0.5f;
            var halfZ = v.Z * 0.5f;

            var sx = (float)Math.Sin(halfX);
            var cx = (float)Math.Cos(halfX);
            var sy = (float)Math.Sin(halfY);
            var cy = (float)Math.Cos(halfY);
            var sz = (float)Math.Sin(halfZ);
            var cz = (float)Math.Cos(halfZ);

            return new Quaternion(
                sx * cy * cz - cx * sy * sz,
                cx * sy * cz + sx * cy * sz,
                cx * cy * sz - sx * sy * cz,
                cx * cy * cz + sx * sy * sz
            );
        }

        public static class Exporter
        {

            public static void Export(string path, IImported imported, bool eulerFilter, float filterPrecision,
                bool allNodes, bool skins, bool animation, bool blendShape, bool castToBone, float boneSize, bool exportAllUvsAsDiffuseMaps, float scaleFactor, int versionIndex, bool isAscii)
            {
                if (!nativeAvailable)
                {
                    throw new NotSupportedException("FBX native runtime is unavailable (AssetStudioFBXNative.dll was not found or failed to load).");
                }

                var file = new FileInfo(path);
                var dir = file.Directory;

                if (!dir.Exists)
                {
                    dir.Create();
                }

                var currentDir = Directory.GetCurrentDirectory();
                Directory.SetCurrentDirectory(dir.FullName);

                var name = Path.GetFileName(path);

                using (var exporter = new FbxExporter(name, imported, allNodes, skins, castToBone, boneSize, exportAllUvsAsDiffuseMaps, scaleFactor, versionIndex, isAscii))
                {
                    exporter.Initialize();
                    exporter.ExportAll(blendShape, animation, eulerFilter, filterPrecision);
                }

                Directory.SetCurrentDirectory(currentDir);
            }

        }

    }
}

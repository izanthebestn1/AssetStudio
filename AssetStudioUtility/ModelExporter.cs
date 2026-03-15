using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace AssetStudio
{
    public static class ModelExporter
    {
        public static void ExportFbx(string path, IImported imported, bool eulerFilter, float filterPrecision,
            bool allNodes, bool skins, bool animation, bool blendShape, bool castToBone, float boneSize, bool exportAllUvsAsDiffuseMaps, float scaleFactor, int versionIndex, bool isAscii)
        {
            if (!Fbx.IsNativeAvailable)
            {
                throw new NotSupportedException("FBX export requires AssetStudioFBXNative.dll, but the native runtime is unavailable.");
            }

            Fbx.Exporter.Export(path, imported, eulerFilter, filterPrecision, allNodes, skins, animation, blendShape, castToBone, boneSize, exportAllUvsAsDiffuseMaps, scaleFactor, versionIndex, isAscii);
        }

        public static bool ExportObj(string outputDirectory, string baseName, IImported imported, out string outputObjPath)
        {
            outputObjPath = null;
            if (imported?.MeshList == null || imported.MeshList.Count == 0)
            {
                return false;
            }

            Directory.CreateDirectory(outputDirectory);

            var safeBaseName = SanitizeFileName(baseName);
            if (string.IsNullOrEmpty(safeBaseName))
            {
                safeBaseName = "model";
            }

            outputObjPath = Path.Combine(outputDirectory, safeBaseName + ".obj");
            var mtlPath = Path.Combine(outputDirectory, safeBaseName + ".mtl");

            var materialNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var obj = new StringBuilder(1024 * 128);
            obj.AppendLine("# AssetStudio OBJ fallback export");
            obj.AppendLine($"mtllib {Path.GetFileName(mtlPath)}");

            var vertexOffset = 1;
            foreach (var mesh in imported.MeshList)
            {
                var meshName = SanitizeObjectName(mesh.Path);
                obj.AppendLine($"o {meshName}");

                for (var i = 0; i < mesh.VertexList.Count; i++)
                {
                    var v = mesh.VertexList[i].Vertex;
                    obj.AppendLine(FormattableString.Invariant($"v {v.X} {v.Y} {v.Z}"));
                }

                var hasUv0 = mesh.hasUV != null && mesh.hasUV.Length > 0 && mesh.hasUV[0];
                for (var i = 0; i < mesh.VertexList.Count; i++)
                {
                    if (hasUv0 && mesh.VertexList[i].UV != null && mesh.VertexList[i].UV.Length > 0 && mesh.VertexList[i].UV[0] != null && mesh.VertexList[i].UV[0].Length >= 2)
                    {
                        var uv = mesh.VertexList[i].UV[0];
                        obj.AppendLine(FormattableString.Invariant($"vt {uv[0]} {1f - uv[1]}"));
                    }
                    else
                    {
                        obj.AppendLine("vt 0 0");
                    }
                }

                var hasNormals = mesh.hasNormal;
                for (var i = 0; i < mesh.VertexList.Count; i++)
                {
                    if (hasNormals)
                    {
                        var n = mesh.VertexList[i].Normal;
                        obj.AppendLine(FormattableString.Invariant($"vn {n.X} {n.Y} {n.Z}"));
                    }
                    else
                    {
                        obj.AppendLine("vn 0 0 1");
                    }
                }

                for (var subMeshIndex = 0; subMeshIndex < mesh.SubmeshList.Count; subMeshIndex++)
                {
                    var subMesh = mesh.SubmeshList[subMeshIndex];
                    var groupName = SanitizeObjectName($"{meshName}_{subMeshIndex}");
                    obj.AppendLine($"g {groupName}");

                    if (!string.IsNullOrEmpty(subMesh.Material))
                    {
                        var materialName = SanitizeObjectName(subMesh.Material);
                        obj.AppendLine($"usemtl {materialName}");
                        materialNames.Add(materialName);
                    }

                    var baseVertex = subMesh.BaseVertex;
                    foreach (var face in subMesh.FaceList)
                    {
                        if (face.VertexIndices == null || face.VertexIndices.Length < 3)
                        {
                            continue;
                        }

                        var a = vertexOffset + baseVertex + face.VertexIndices[0];
                        var b = vertexOffset + baseVertex + face.VertexIndices[1];
                        var c = vertexOffset + baseVertex + face.VertexIndices[2];
                        obj.AppendLine($"f {a}/{a}/{a} {b}/{b}/{b} {c}/{c}/{c}");
                    }
                }

                vertexOffset += mesh.VertexList.Count;
            }

            File.WriteAllText(outputObjPath, obj.ToString());
            WriteMaterialFile(mtlPath, imported, materialNames);
            ExportTextureFiles(outputDirectory, imported);
            return true;
        }

        private static void WriteMaterialFile(string mtlPath, IImported imported, HashSet<string> materialNames)
        {
            var mtl = new StringBuilder(8192);
            mtl.AppendLine("# AssetStudio OBJ fallback materials");

            var usedMaterials = imported.MaterialList
                .Where(m => m != null && !string.IsNullOrEmpty(m.Name) && materialNames.Contains(SanitizeObjectName(m.Name)))
                .ToList();

            foreach (var material in usedMaterials)
            {
                var materialName = SanitizeObjectName(material.Name);
                mtl.AppendLine($"newmtl {materialName}");
                mtl.AppendLine(FormattableString.Invariant($"Ka {ToColor01(material.Ambient.R)} {ToColor01(material.Ambient.G)} {ToColor01(material.Ambient.B)}"));
                mtl.AppendLine(FormattableString.Invariant($"Kd {ToColor01(material.Diffuse.R)} {ToColor01(material.Diffuse.G)} {ToColor01(material.Diffuse.B)}"));
                mtl.AppendLine(FormattableString.Invariant($"Ks {ToColor01(material.Specular.R)} {ToColor01(material.Specular.G)} {ToColor01(material.Specular.B)}"));
                mtl.AppendLine(FormattableString.Invariant($"d {1f - material.Transparency}"));

                var textureName = ResolveTextureName(material, imported.TextureList);
                if (!string.IsNullOrEmpty(textureName))
                {
                    mtl.AppendLine($"map_Kd {textureName}");
                }

                mtl.AppendLine();
            }

            File.WriteAllText(mtlPath, mtl.ToString());
        }

        private static void ExportTextureFiles(string outputDirectory, IImported imported)
        {
            if (imported.TextureList == null)
            {
                return;
            }

            foreach (var texture in imported.TextureList)
            {
                if (texture?.Data == null || texture.Data.Length == 0)
                {
                    continue;
                }

                var textureName = ResolveTextureFileName(texture.Name);
                var texturePath = Path.Combine(outputDirectory, textureName);
                if (!File.Exists(texturePath))
                {
                    File.WriteAllBytes(texturePath, texture.Data);
                }
            }
        }

        private static string ResolveTextureName(ImportedMaterial material, List<ImportedTexture> textures)
        {
            if (material.Textures == null || textures == null)
            {
                return null;
            }

            foreach (var matTex in material.Textures)
            {
                if (string.IsNullOrEmpty(matTex?.Name))
                {
                    continue;
                }

                var texture = textures.FirstOrDefault(t => string.Equals(t.Name, matTex.Name, StringComparison.OrdinalIgnoreCase));
                if (texture != null)
                {
                    return ResolveTextureFileName(texture.Name);
                }
            }

            return null;
        }

        private static string ResolveTextureFileName(string textureName)
        {
            var safe = SanitizeFileName(textureName);
            if (string.IsNullOrEmpty(Path.GetExtension(safe)))
            {
                safe += ".png";
            }

            return safe;
        }

        private static float ToColor01(float channel)
        {
            if (channel < 0f)
            {
                return 0f;
            }

            if (channel > 1f)
            {
                return 1f;
            }

            return channel;
        }

        private static string SanitizeObjectName(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "unnamed";
            }

            var sanitized = value.Replace(' ', '_').Replace('/', '_').Replace('\\', '_');
            return SanitizeFileName(sanitized);
        }

        private static string SanitizeFileName(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var chars = value.ToCharArray();
            for (var i = 0; i < chars.Length; i++)
            {
                if (Array.IndexOf(Path.GetInvalidFileNameChars(), chars[i]) >= 0)
                {
                    chars[i] = '_';
                }
            }

            return new string(chars);
        }
    }
}

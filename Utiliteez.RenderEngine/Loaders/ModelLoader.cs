using Assimp;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using Utiliteez.RenderEngine;
using Utiliteez.RenderEngine.Structs;
using Material = Utiliteez.RenderEngine.Structs.Material;

namespace Utiliteez.Render
{
    
    
    public class ModelLoader: ILoader<Model>
    {
        public Model Load(string filePath, uint matIndexOffset)
        {
            using (var importer = new AssimpContext())
            {
                var scene = importer.ImportFile(filePath,
                    PostProcessSteps.Triangulate |
                    PostProcessSteps.GenerateNormals |
                    PostProcessSteps.FlipUVs);

                var vertices = new List<Vertex>();
                var indices  = new List<uint>();
                var materialMap = new Dictionary<Material, int>();
                var mats        = new List<Material>();

                foreach (var mesh in scene.Meshes)
                {
                    // 1) Build the key
                    var mat = scene
                        .Materials[mesh.MaterialIndex]
                        .ToMaterialEntry();  // your IEquatable type

                    // 2) Deduplicate via TryGetValue
                    if (!materialMap.TryGetValue(mat, out var matIdx))
                    {
                        matIdx = mats.Count;
                        materialMap[mat] = matIdx;
                        mats.Add(mat);
                    }

                    // 3) Remember where this mesh’s vertices start
                    int baseIndex = vertices.Count;
                    
                    // 4) Append vertices, stamping in the material index
                    for (int i = 0; i < mesh.Vertices.Count; i++)
                    {
                        var uv = mesh.HasTextureCoords(0)
                            ? mesh.TextureCoordinateChannels[0][i].ToVector2()
                            : Vector2.Zero;
                        
                        //mats[matIdx].UpdateRect(uv);
                        vertices.Add(new Vertex {
                            Position      = mesh.Vertices[i].ToVector3(),
                            Normal        = mesh.Normals[i].ToVector3(),
                            Uv            = uv, 
                            MaterialIndex = (uint)matIdx + matIndexOffset
                        });
                    }

                    // 5) Offset and append indices
                    foreach (var face in mesh.Faces)
                    foreach (var localIdx in face.Indices)
                        indices.Add((uint)(baseIndex + localIdx));
                }

                // 6) Return everything
                return new Model {
                    Vertices = vertices.ToArray(),
                    Indices = indices .ToArray(),
                    Materials = mats.ToArray()
                }; 
            }
            
        }

    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawMeshInstancedDemo : MonoBehaviour
{
   // How many meshes to draw.
   public int population;
   // Range to draw meshes within.
   public float range;
 
   // Material to use for drawing the meshes.
   public Material material;
 
   private Matrix4x4[] matrices;
   private MaterialPropertyBlock block;
 
   private Mesh mesh;

   private Matrix4x4 mat;
 
   private void Setup() {
      Mesh mesh = CreateQuad();
      this.mesh = mesh;
 
      matrices = new Matrix4x4[population];
      Vector4[] colors = new Vector4[population];
 
      block = new MaterialPropertyBlock();
 
      for (int i = 0; i < population; i++) {
         // Build matrix.
         Vector3 position = new Vector3(Random.Range(-range, range), Random.Range(-range, range), Random.Range(-range, range));
         Quaternion rotation = Quaternion.Euler(Random.Range(-180, 180), Random.Range(-180, 180), Random.Range(-180, 180));
         Vector3 scale = Vector3.one;
 
         mat = Matrix4x4.TRS(position, rotation, scale);
 
         matrices[i] = mat;
 
         colors[i] = Color.Lerp(Color.red, Color.blue, Random.value);
      }
 
      // Custom shader needed to read these!!
      block.SetVectorArray("_Colors", colors);
   }
 
   private Mesh CreateQuad(float width = 1f, float height = 1f) {
      // Create a quad mesh.
      // See source for implementation.
      return null;
   }
 
   private void Start() {
      Setup();
   }
 
   private void Update() {
      // Draw a bunch of meshes each frame.
      Graphics.DrawMeshInstanced(mesh, 0, material, matrices, population, block);
   }
}

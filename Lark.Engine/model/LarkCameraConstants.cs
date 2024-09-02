using System.Runtime.InteropServices;
using Silk.NET.Maths;

namespace Lark.Engine.model;

[StructLayout(LayoutKind.Sequential)]
public struct LarkCameraConstants {
  public Matrix4X4<float> InvertView;
  public Matrix4X4<float> InvertProjection;
  public Vector4D<float> CameraPosition;
  public int lightIndex;
}
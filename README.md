# Pannellum in C# #

Pannellum Library binding for C# WPF

***

## Dependencies
- OpenTK (>= 3.0.0-pre)
- OpenTK.GLControl (>= 3.0.0-pre)
- OpenCvSharp3 (for reading video frame)

***

## Documentation
```csharp
class EquirecToRect
```

```csharp
public float Pitch { get; set; }
```
Pitch to render at (in radians)

```csharp
public float Yaw { get; set; }
```
Yaw to render at (in radians)

```csharp
public float Roll { get; set; }
```
Camera Roll (in radians)

```csharp
public EquirectToRect(GLControl viewer, System.Drawing.Size size, float haov, float vaov, float voffset, float hfov)
```
- viewer : GLControl object
- size : Size of the viewer
- haov : Horizontal angle of view (in radians)
- vaov : Vertical angle of view (in radians)
- voffset : Vertial offset (in radians)
- hfov : Horizontal field of view (in radians) (recommend 100 * Math.PI / 180)

```csharp
public void Render(OpenCvSharp.Mat frame)
```
- frame : the frame to be rendered

Render the scene based on properties (Pitch, Yaw, Roll, etc...)

All properties are re-calculated to ensure if they are in valid ranges

This method also makes the viewer current context and performs swapping buffers
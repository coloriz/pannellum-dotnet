# Pannellum .NET

[Pannellum Library](https://github.com/mpetroff/pannellum) written in C# (only part of).

uses WPF as a viewer.

## Dependencies

- OpenTK (>= 3.0.0-pre)
- OpenTK.GLControl (>= 3.0.0-pre)
- OpenCvSharp3 (for reading video frame)

## Projects

### Pannellum

Panorama rendering library written in C#, it requires OpenTK to make a GL surface in WPF controls. The original project is [https://github.com/mpetroff/pannellum](https://github.com/mpetroff/pannellum).

#### 📃 Documentation

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
Initialize renderer.

- viewer : GLControl object
- size : Size of the viewer
- haov : Horizontal angle of view (in radians)
- vaov : Vertical angle of view (in radians)
- voffset : Vertial offset (in radians)
- hfov : Horizontal field of view (in radians) (recommend 100 * Math.PI / 180)

```csharp
public void Render(OpenCvSharp.Mat frame)
```
Render the scene based on properties (Pitch, Yaw, Roll, etc...).
All properties are re-calculated to ensure if they are in valid ranges.
This method also makes the viewer current context and performs swapping buffers.

- frame : the frame to be rendered

### Pannellum_example

**Controls**

- ⬅➡ : Yaw
- ⬆⬇ : Pitch
- Page Up, Page Down : Roll

**Demo**

![pannellum_demo](docs/pannellum_example.gif)

### FoveInteractiveView

Render the scene based on the pose information of a Fove HMD.

**Demo**

TBD.
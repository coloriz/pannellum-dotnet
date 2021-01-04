using System;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;

namespace Pannellum
{
    /// <summary>
    /// The renderer class.
    /// Contains all methods for projecting Equirectangular frame to Rectilinear frame
    /// </summary>
    public class EquirectToRect
    {
        private readonly string vertexSrc = String.Join(Environment.NewLine,
            "#version 120",
            "attribute vec2 a_texCoord;",
            "varying vec2 v_texCoord;",
            "void main() {",
            // Set position
            "	gl_Position = vec4(a_texCoord, 0.0, 1.0);",
            // Pass the coordinates to the fragment shader
            "	v_texCoord = a_texCoord;",
            "}"
        );
        private readonly string fragmentSrc = String.Join(Environment.NewLine,
            "#version 120",
            "precision mediump float;",

            "uniform float u_aspectRatio;",
            "uniform float u_psi;",
            "uniform float u_theta;",
            "uniform float u_f;",
            "uniform float u_h;",
            "uniform float u_v;",
            "uniform float u_vo;",
            "uniform float u_rot;",

            "const float PI = 3.14159265358979323846264;",

            // Texture
            "uniform sampler2D u_image;",
            "uniform samplerCube u_imageCube;",

            // Coordinates passed in from vertex shader
            "varying vec2 v_texCoord;",

            // Background color (display for partial panoramas)
            "uniform vec4 u_backgroundColor;",

            "void main() {",
            // Map canvas/camera to sphere
            "	float x = v_texCoord.x * u_aspectRatio;",
            "	float y = v_texCoord.y;",
            "	float sinrot = sin(u_rot);",
            "	float cosrot = cos(u_rot);",
            "	float rot_x = x * cosrot - y * sinrot;",
            "	float rot_y = x * sinrot + y * cosrot;",
            "	float sintheta = sin(u_theta);",
            "	float costheta = cos(u_theta);",
            "	float a = u_f * costheta - rot_y * sintheta;",
            "	float root = sqrt(rot_x * rot_x + a * a);",
            "	float lambda = atan(rot_x / root, a / root) + u_psi;",
            "	float phi = atan((rot_y * costheta + u_f * sintheta) / root);",
            // Wrap image
            "	lambda = mod(lambda + PI, PI * 2.0) - PI;",

            // Map texture to sphere
            "	vec2 coord = vec2(lambda / PI, phi / (PI / 2.0));",

            // Look up color from texture
            // Map from [-1,1] to [0,1] and flip y-axis
            "	if(coord.x < -u_h || coord.x > u_h || coord.y < -u_v + u_vo || coord.y > u_v + u_vo)",
            "		gl_FragColor = u_backgroundColor;",
            "	else",
            "		gl_FragColor = texture2D(u_image, vec2((coord.x + u_h) / (u_h * 2.0), (-coord.y + u_v + u_vo) / (u_v * 2.0)));",
            "}");
        public GLControl Viewer { get; set; }
        private struct GLProgram
        {
            public int programID;
            public int texCoordLocation;
            public int aspectRatio;
            public int psi;
            public int theta;
            public int f;
            public int h;
            public int v;
            public int vo;
            public int rot;
            public int backgroundColor;
            public int texture;
        };
        GLProgram glProgram;
        private const TextureTarget glBindType = TextureTarget.Texture2D;
        private int texCoordVertexArray;
        private int texCoordBuffer;
        private int vertexShader;
        private int fragmentShader;

        public float Haov { get; set; }
        public float Vaov { get; set; }
        public float Voffset { get; set; }
        public float Hfov { get; set; }

        /// <summary>
        /// Pitch to render at (in radians)
        /// </summary>
        public float Pitch { get; set; } = 0;

        /// <summary>
        /// Yaw to render at (in radians)
        /// </summary>
        public float Yaw { get; set; } = 0;

        /// <summary>
        /// Camera Roll (in radians)
        /// </summary>
        public float Roll { get; set; } = 0;

        private System.Drawing.Size renderSize;

        /// <summary>
        /// Initialize renderer
        /// </summary>
        /// <param name="viewer">GLcontrol object to draw a rectlinear frame</param>
        /// <param name="size">Size of the canvas (viewer)</param>
        /// <param name="haov">Horizontal angle of view (in radians)</param>
        /// <param name="vaov">Vertical angle of view (in radians)</param>
        /// <param name="voffset">Vertical offset angle (in radians)</param>
        /// <param name="hfov">Horizontal field of view to render with (in radians)</param>
        public EquirectToRect(GLControl viewer, System.Drawing.Size size, float haov, float vaov, float voffset, float hfov)
        {
            Viewer = viewer;
            renderSize = size;
            Haov = haov;
            Vaov = vaov;
            Voffset = voffset;
            Hfov = hfov;

            // Make this context current
            Viewer.MakeCurrent();

            // Create viewport for entire canvas
            GL.Viewport(size);

            // Create vertex shader
            vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, vertexSrc);
            GL.CompileShader(vertexShader);

            // Create fragment shader
            fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, fragmentSrc);
            GL.CompileShader(fragmentShader);

            // Link OpenGL program
            glProgram.programID = GL.CreateProgram();
            GL.AttachShader(glProgram.programID, vertexShader);
            GL.AttachShader(glProgram.programID, fragmentShader);
            GL.LinkProgram(glProgram.programID);

            // Use OpenGL program
            GL.UseProgram(glProgram.programID);

            // Look up texture coordinates location
            glProgram.texCoordLocation = GL.GetAttribLocation(glProgram.programID, "a_texCoord");

            // Provide texture coordinates for rectangle
            texCoordVertexArray = GL.GenVertexArray();
            GL.BindVertexArray(texCoordVertexArray);
            GL.EnableVertexAttribArray(glProgram.texCoordLocation);

            texCoordBuffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, texCoordBuffer);
            float[] texCoordBufferData = { -1f, 1f, 1f, 1f, 1f, -1f, -1f, 1f, 1f, -1f, -1f, -1f };
            GL.BufferData<float>(BufferTarget.ArrayBuffer, sizeof(float) * texCoordBufferData.Length, texCoordBufferData, BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(glProgram.texCoordLocation, 2, VertexAttribPointerType.Float, false, 0, 0);

            // Pass aspect ratio
            glProgram.aspectRatio = GL.GetUniformLocation(glProgram.programID, "u_aspectRatio");
            GL.Uniform1(glProgram.aspectRatio, (float)renderSize.Width / (float)renderSize.Height);

            // Locate psi, theta, focal length, horizontal extent, vertical extent, and vertical offset
            glProgram.psi = GL.GetUniformLocation(glProgram.programID, "u_psi");
            glProgram.theta = GL.GetUniformLocation(glProgram.programID, "u_theta");
            glProgram.f = GL.GetUniformLocation(glProgram.programID, "u_f");
            glProgram.h = GL.GetUniformLocation(glProgram.programID, "u_h");
            glProgram.v = GL.GetUniformLocation(glProgram.programID, "u_v");
            glProgram.vo = GL.GetUniformLocation(glProgram.programID, "u_vo");
            glProgram.rot = GL.GetUniformLocation(glProgram.programID, "u_rot");

            // Pass horizontal extent, vertical extent, and vertical offset
            GL.Uniform1(glProgram.h, Haov / ((float)Math.PI * 2f));
            GL.Uniform1(glProgram.v, Vaov / (float)Math.PI);
            GL.Uniform1(glProgram.vo, Voffset / (float)Math.PI * 2f);

            // Set background color
            glProgram.backgroundColor = GL.GetUniformLocation(glProgram.programID, "u_backgroundColor");
            GL.Uniform4(glProgram.backgroundColor, new Color4(0f, 0f, 0f, 1f));

            // Create texture
            GL.CreateTextures(glBindType, 1, out glProgram.texture);
            GL.BindTexture(glBindType, glProgram.texture);

            // Set parameters for rendering any size
            GL.TexParameter(glBindType, TextureParameterName.TextureWrapS, (int)TextureParameterName.ClampToEdge);
            GL.TexParameter(glBindType, TextureParameterName.TextureWrapT, (int)TextureParameterName.ClampToEdge);
            GL.TexParameter(glBindType, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(glBindType, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        }

        /// <summary>
        /// Initialize renderer with default values of haov = 2π, vaov = π and voffset = 0
        /// </summary>
        /// <param name="viewer">GLcontrol object to draw a rectlinear frame</param>
        /// <param name="size">Size of the canvas (viewer)</param>
        /// <param name="hfov">Horizontal field of view to render with (in radians)</param>
        public EquirectToRect(GLControl viewer, System.Drawing.Size size, float hfov) : this(viewer, size, (float)(360 * Math.PI / 180), (float)(180 * Math.PI / 180), 0, hfov) { }

        /// <summary>
        /// Render the scene based on properties (Pitch, Yaw, Roll, etc...)
        /// All properties are re-calculated to ensure if they are in valid ranges
        /// This method also makes the viewer current context and performs swapping buffers
        /// </summary>
        /// <param name="frame"></param>
        public void Render(OpenCvSharp.Mat frame)
        {
            // Ensure pitch is within min and max allowed
            Pitch = Math.Max(-90, Math.Min(90, Pitch));

            // Ensure the yaw is within min and max allowed
            if (Yaw > (float)Math.PI) Yaw -= 2 * (float)Math.PI;
            else if (Yaw < -(float)Math.PI) Yaw += 2 * (float)Math.PI;

            Yaw = Math.Max(-180, Math.Min(180, Yaw));

            // Make this context current
            Viewer.MakeCurrent();

            // Upload image to the texture
            GL.TexImage2D(glBindType, 0, PixelInternalFormat.Rgb, frame.Cols, frame.Rows, 0, PixelFormat.Bgr, PixelType.UnsignedByte, frame.Data);

            // Calculate focal length from vertical field of view
            float vfov = 2 * (float)Math.Atan(Math.Tan(Hfov * 0.5) / ((float)renderSize.Width / (float)renderSize.Height));
            float focal = 1 / (float)Math.Tan(vfov * 0.5);

            // Pass psi, theta, roll, and focal length
            GL.Uniform1(glProgram.psi, Yaw);
            GL.Uniform1(glProgram.theta, Pitch);
            GL.Uniform1(glProgram.rot, Roll);
            GL.Uniform1(glProgram.f, focal);

            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
            Viewer.SwapBuffers();
        }

        ~EquirectToRect()
        {
            GL.DeleteTexture(glProgram.texture);
            GL.DeleteBuffer(texCoordBuffer);
            GL.DeleteVertexArray(texCoordVertexArray);
            GL.DetachShader(glProgram.programID, vertexShader);
            GL.DetachShader(glProgram.programID, fragmentShader);
            GL.DeleteProgram(glProgram.programID);
        }
    }
}

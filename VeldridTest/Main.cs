using System.Numerics;
using System.Text;
using Veldrid;
using Veldrid.SPIRV;
using Veldrid.StartupUtilities;

namespace VeldridTest;

public class Main
{
    GraphicsDevice _graphicsDevice = default!;
    CommandList _commandList = default!;
    DeviceBuffer _vertexBuffer = default!;
    DeviceBuffer _indexBuffer = default!;
    Shader[] _shaders = default!;
    Pipeline _pipeline = default!;
    
    const string VertexCode = @"
#version 450
layout(location = 0) in vec2 Position;
layout(location = 1) in vec4 Color;
layout(location = 0) out vec4 fsin_Color;
void main()
{
    gl_Position = vec4(Position, 0, 1);
    fsin_Color = Color;
}";

    const string FragmentCode = @"
#version 450
layout(location = 0) in vec4 fsin_Color;
layout(location = 0) out vec4 fsout_Color;
void main()
{
    fsout_Color = fsin_Color;
}";
    
    public void Run()
    {
        var windowCi = new WindowCreateInfo()
        {
            X = 100,
            Y = 100,
            WindowWidth = 960,
            WindowHeight = 540,
            WindowTitle = "Veldrid Tutorial"
        };

        var window = VeldridStartup.CreateWindow(ref windowCi);

        var options = new GraphicsDeviceOptions
        {
            PreferStandardClipSpaceYDirection = true,
            PreferDepthRangeZeroToOne = true
        };

        _graphicsDevice = VeldridStartup.CreateGraphicsDevice(window, options);

        CreateResources();
        
        while (window.Exists)
        {
            window.PumpEvents();
            Draw();
        }

        DisposeResources();
    }

    void Draw()
    {
        _commandList.Begin();
        _commandList.SetFramebuffer(_graphicsDevice.SwapchainFramebuffer);
        _commandList.ClearColorTarget(0, RgbaFloat.Black);
        
        _commandList.SetVertexBuffer(0, _vertexBuffer);
        _commandList.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
        _commandList.SetPipeline(_pipeline);
        _commandList.DrawIndexed(
            indexCount: 4,
            instanceCount: 1,
            indexStart: 0,
            vertexOffset: 0,
            instanceStart: 0);
        
        _commandList.End();
        _graphicsDevice.SubmitCommands(_commandList);
        _graphicsDevice.SwapBuffers();
    }

    void CreateResources()
    {
        var factory = _graphicsDevice.ResourceFactory;
        
        VertexPositionColor[] quadVertices =
        {
            new(new Vector2(-0.75f, 0.75f), RgbaFloat.Red),
            new(new Vector2(0.75f, 0.75f), RgbaFloat.Green),
            new(new Vector2(-0.75f, -0.75f), RgbaFloat.Blue),
            new(new Vector2(0.75f, -0.75f), RgbaFloat.Yellow)
        };
        
        ushort[] quadIndices = { 0, 1, 2, 3 };
        
        _vertexBuffer = factory.CreateBuffer(new BufferDescription(4 * VertexPositionColor.SizeInBytes, BufferUsage.VertexBuffer));
        _indexBuffer = factory.CreateBuffer(new BufferDescription(4 * sizeof(ushort), BufferUsage.IndexBuffer));
        
        _graphicsDevice.UpdateBuffer(_vertexBuffer, 0, quadVertices);
        _graphicsDevice.UpdateBuffer(_indexBuffer, 0, quadIndices);
        
        var vertexLayout = new VertexLayoutDescription(
            new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
            new VertexElementDescription("Color", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4));
        
        var vertexShaderDesc = new ShaderDescription(
            ShaderStages.Vertex,
            Encoding.UTF8.GetBytes(VertexCode),
            "main");
        var fragmentShaderDesc = new ShaderDescription(
            ShaderStages.Fragment,
            Encoding.UTF8.GetBytes(FragmentCode),
            "main");

        _shaders = factory.CreateFromSpirv(vertexShaderDesc, fragmentShaderDesc);
        
        var pipelineDescription = new GraphicsPipelineDescription
        {
            BlendState = BlendStateDescription.SingleOverrideBlend,
            DepthStencilState = new DepthStencilStateDescription(
                depthTestEnabled: true,
                depthWriteEnabled: true,
                comparisonKind: ComparisonKind.LessEqual),
            RasterizerState = new RasterizerStateDescription(
                cullMode: FaceCullMode.Back,
                fillMode: PolygonFillMode.Solid,
                frontFace: FrontFace.Clockwise,
                depthClipEnabled: true,
                scissorTestEnabled: false),
            PrimitiveTopology = PrimitiveTopology.TriangleStrip,
            ResourceLayouts = Array.Empty<ResourceLayout>(),
            ShaderSet = new ShaderSetDescription(
                vertexLayouts: new[] { vertexLayout },
                shaders: _shaders),
            Outputs = _graphicsDevice.SwapchainFramebuffer.OutputDescription
        };

        _pipeline = factory.CreateGraphicsPipeline(pipelineDescription);
        
        _commandList = factory.CreateCommandList();
    }
    
    void DisposeResources()
    {
        _pipeline.Dispose();
        _commandList.Dispose();
        _vertexBuffer.Dispose();
        _indexBuffer.Dispose();
        _graphicsDevice.Dispose();
    }
}

struct VertexPositionColor
{
    public Vector2 Position; // This is the position, in normalized device coordinates.
    public RgbaFloat Color; // This is the color of the vertex.
    public VertexPositionColor(Vector2 position, RgbaFloat color)
    {
        Position = position;
        Color = color;
    }
    public const uint SizeInBytes = 24;
}
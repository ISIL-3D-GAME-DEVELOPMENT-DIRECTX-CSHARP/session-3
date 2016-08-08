﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SharpDX;
using SharpDX.Windows;
using SharpDX.DXGI;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.D3DCompiler;

using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

namespace Sesion2_Lab01 {
    public class NativeApplication {

        private const int App_Width = 800;
        private const int App_Height = 600;

        // Es como el Windows.Forms, pero este se utiliza nativamente para el DirectX
        // Se necesita las librerias:
        //  1. System.Windows.Forms
        //  2. SharpDX.Windows
        private RenderForm mRenderForm;

        private Texture2D mBackBufferFBO;
        private RenderTargetView mRenderTargetView;
        private Device mDevice;
        private DeviceContext mDeviceContext;

        private Factory mFactory;

        private RenderCamera mRenderCamera;
        private ShaderProgram mShaderProgram;

        private SwapChain mSwapChain;
        private SwapChainDescription mSwapChainDescription;

        public NativeApplication() {
            mRenderForm = new RenderForm("Sesion 2::Aplicacion Nativa DirectX 11");

            mSwapChainDescription = new SwapChainDescription(); // es una estructura, no es necesario construirlo
            mSwapChainDescription.BufferCount = 1;
            mSwapChainDescription.IsWindowed = true; // es ventana
            mSwapChainDescription.SwapEffect = SwapEffect.Discard;
            mSwapChainDescription.Usage = Usage.RenderTargetOutput;
            mSwapChainDescription.OutputHandle = mRenderForm.Handle; // le pasamos el puntero de nuestro RenderForm
            mSwapChainDescription.SampleDescription.Count = 1;
            mSwapChainDescription.SampleDescription.Quality = 0;
            mSwapChainDescription.ModeDescription.Width = NativeApplication.App_Width;  // aqui definimos el ancho de la ventana
            mSwapChainDescription.ModeDescription.Height = NativeApplication.App_Height; // aqui definimos el alto de la ventana
            mSwapChainDescription.ModeDescription.RefreshRate.Numerator = 60; // aqui definimos el Frame Rate
            mSwapChainDescription.ModeDescription.RefreshRate.Denominator = 1; // siempre por defecto 1... la matematica es Denominator / Numerator
            mSwapChainDescription.ModeDescription.Format = Format.R8G8B8A8_UNorm; // se define el formato del color de la ventana

            // Creamos la tarjeta grafica usando el SwapChainDescription
            // Esto nos devuelve:
            //  1. Device -> La tarjeta grafica
            //  2. SwapChain -> Control del Frame Rate
            Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.None, mSwapChainDescription,
                out mDevice, out mSwapChain);

            // recogemos el contexto de la tarjeta de video
            mDeviceContext = mDevice.ImmediateContext;

            // recogemos el padre constructor de la ventana
            mFactory = mSwapChain.GetParent<Factory>();
            // ignoramos todos los eventos de la ventana
            mFactory.MakeWindowAssociation(mRenderForm.Handle, WindowAssociationFlags.IgnoreAll);

            // creamos un frame buffer object, y usamos este back buffer para almacenar la data del render
            mBackBufferFBO = Texture2D.FromSwapChain<Texture2D>(mSwapChain, 0);

            // creamos un render target view para manejar el render usando nuestro frame buffer object
            mRenderTargetView = new RenderTargetView(mDevice, mBackBufferFBO);

            // cargamos y construimos nuestro Shader
            mShaderProgram = new ShaderProgram(mDevice);
            mShaderProgram.Load("Content/Fx_Primitive.fx");

            // Ahora inicializamos algunos datos para poder dibujar
            PreConfiguration();

            // Ahora creamos algo muy importante! Nuestro Render Loop, donde ira nuestro Draw y Update!
            RenderLoop.Run(mRenderForm, OnRenderLoop);
        }
        
        // variables para dibujar nuestro primitivo
        private ushort[] mIndices;
        private Vector4[] mVertices;
        private VertexBufferBinding mVertexBufferBinding;

        private void PreConfiguration() {
            // preparamos nuestros parametros para dibujar
            // creamos una camara para el escenario
            mRenderCamera = new RenderCamera(NativeApplication.App_Width, NativeApplication.App_Height);

            // Creamos nuestro Viewport que define el tamanio de nuestras dimension de dibujo para
            // el DirectX
            mDeviceContext.Rasterizer.SetViewports(new Viewport(0, 0, NativeApplication.App_Width,
                NativeApplication.App_Height, 0.0f, 1.0f));

            // ahora pasamos nuestro Frame Buffer Object
            mDeviceContext.OutputMerger.SetTargets(mRenderTargetView);

            // creamos nuestro indices
            mIndices = new ushort[3];
            mIndices[0] = 0;
            mIndices[1] = 1;
            mIndices[2] = 2;

            // creamos nustros vertices
            mVertices = new Vector4[6];
            // nuestro primer vertice
            mVertices[0] = new Vector4(0.0f, 0f, 0f, 1.0f); 
            mVertices[1] = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
            // nuestro segundo vertice
            mVertices[2] = new Vector4(150f, 0f, 0f, 1.0f);
            mVertices[3] = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
            // nuestro tercer vertice
            mVertices[4] = new Vector4(150f, 150f, 0f, 1.0f);
            mVertices[5] = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
        }

        private void OnRenderLoop() {
            Update();
            Draw();
        }

        private Buffer mVertexBuffer;
        private Buffer mIndexBuffer;

        private void Update() {
            // actualizamos nuestra camara
            mRenderCamera.Update();

            // ahora creamos nuestro Buffer para poder almacenar los Vertice de una manera
            // que la tarjeta de video pueda leer y transferir los vertices a los Shaders
            if (mVertexBuffer != null) {
                mVertexBuffer.Dispose();
                mVertexBuffer = null;
            }

            mVertexBuffer = Buffer.Create(mDevice, BindFlags.VertexBuffer, mVertices);

            // ahora mandamos nuestro Buffer a la tarjeta de video para que lo pueda transferir 
            // al Shader
            mVertexBufferBinding.Buffer = mVertexBuffer;
            mVertexBufferBinding.Stride = 32;
            mVertexBufferBinding.Offset = 0;

            // mandamos vertices al GPU
            mDeviceContext.InputAssembler.SetVertexBuffers(0, mVertexBufferBinding);

            // ahora vamos a definir nuestros vertices y mandar al GPU
            if (mIndexBuffer != null) {
                mIndexBuffer.Dispose();
                mIndexBuffer = null;
            }

            mIndexBuffer = Buffer.Create(mDevice, BindFlags.IndexBuffer, mIndices);

            mDeviceContext.InputAssembler.SetIndexBuffer(mIndexBuffer, Format.R16_UInt, 0);
        }

        private void Draw() {
            // aqui definimos nuestro color usando Color4
            Color4 clearColor = Color4.Black;
            clearColor.Red = 0f;
            clearColor.Green = 1f;
            clearColor.Blue = 0f;

            // aqui limpiamos nuestra ventana con un color solido
            mDeviceContext.ClearRenderTargetView(mRenderTargetView, clearColor);

            /////////////////// EMPEZAMOS A DIBUJAR NUESTRO PRIMITIVO ///////////////

            // ahora transferimos a la tarjeta de video el Input Layout que define los parametros
            // de entrada de nuestro Shader

            // aqui vamos a pasarle de nuestro Shader al GPU nuestra Matrix de transformacion
            Matrix trans = mRenderCamera.transformed;
            trans.Transpose();

            mShaderProgram.Draw(trans);

            mDeviceContext.InputAssembler.InputLayout = mShaderProgram.InputLayout;
            // definimos ahora de que manera se va a tratar la data que entra y como se dibuja
            mDeviceContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            // aqui vamos a transferir el Vertex y Fragment Shader que ya habiamos creado
            mDeviceContext.VertexShader.Set(mShaderProgram.VertexShader);
            mDeviceContext.PixelShader.Set(mShaderProgram.PixelShader);

            // dibujamos nuestros vertices!
            //mDeviceContext.Draw(3, 0);
            mDeviceContext.DrawIndexed(mIndices.Length, 0, 0);

            // hacemos un swap de buffers
            mSwapChain.Present(0, PresentFlags.None);
        }
    }
}

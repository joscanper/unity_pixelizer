Shader "GPUMan/Pixelated"
{
	Properties
	{
		_InitColor("InitColor", Color) = (0,1,0,1)
		_InitColorSpeed("Init Color Speed", Float) = 1
	}

		SubShader{
			Pass
			{
			CGPROGRAM

			#include "UnityCG.cginc"
			#include "UnityLightingCommon.cginc"

			#pragma vertex vert
			#pragma fragment frag
			#pragma geometry geom

			half _VoxelSize;
			half _InitTime;
			half _InitColorSpeed;
			half4 _InitColor;
			float4x4 _ObjWorldMatrix;
			float4x4 _WorldObjMatrix;
			sampler2D _PixelatedTexture;

			struct Voxel
			{
				float3 pos;
				float2 uv;
			};

			StructuredBuffer<Voxel> _Voxels;

			struct v2f {
				float4 pos : SV_POSITION;
				float4 col : COLOR;
				float3 normal : NORMAL;
				float2 uv : TEXCOORD0;
			};

			v2f vert(uint id : SV_VertexID)
			{
				v2f o;
				o.pos = float4(_Voxels[id].pos, 1);
				o.uv = _Voxels[id].uv;
				o.col = tex2Dlod(_PixelatedTexture, float4(o.uv.x, o.uv.y, 0, 0));
				o.normal = 1;
				return o;
			}

			#define	CUBE_VERTS 36

			static half3 cubeVertices[CUBE_VERTS] =
			{
				// Front face
				half3(-1, 0, -1),
				half3(-1, 2, -1),
				half3(1, 2, -1),

				half3(-1, 0, -1),
				half3(1, 2, -1),
				half3(1, 0, -1),

				// Top face
				half3(-1, 2, -1),
				half3(-1, 2, 1),
				half3(1, 2, 1),

				half3(-1, 2, -1),
				half3(1, 2, 1),
				half3(1, 2, -1),

				// Bottom face
				half3(-1, 0, 1),
				half3(-1, 0, -1),
				half3(1, 0, 1),

				half3(1, 0, 1),
				half3(-1, 0, -1),
				half3(1, 0, -1),

				// Back face
				half3(-1, 2, 1),
				half3(-1, 0, 1),
				half3(1, 2, 1),

				half3(1, 2, 1),
				half3(-1, 0, 1),
				half3(1, 0, 1),

				// Right face
				half3(1, 0, -1),
				half3(1, 2, -1),
				half3(1, 2, 1),

				half3(1, 0, -1),
				half3(1, 2, 1),
				half3(1, 0, 1),

				// Left face
				half3(-1, 2, -1),
				half3(-1, 0, -1),
				half3(-1, 2, 1),

				half3(-1, 2, 1),
				half3(-1, 0, -1),
				half3(-1, 0, 1),
			};

			static half3 cubeNormals[6] =
			{
				// Front face
				half3(0, 0, -1),
				half3(0, 1, 0),
				half3(0, -1, 0),

				half3(0, 0, 1),
				half3(1, 0, 0),
				half3(-1, 0, 0),
			};

			[maxvertexcount(CUBE_VERTS)]
			void geom(point v2f input[1], uint pid : SV_PrimitiveID, inout TriangleStream<v2f> outStream)
			{
				float offset = _VoxelSize * 0.5;
				v2f v = input[0];
				float3 localPos = v.pos;

				for (uint i = 0; i < CUBE_VERTS; ++i)
				{
					float3 cubeV = cubeVertices[i];
					float3 localV = localPos + cubeV * offset;
					float4 worldPos = mul(_ObjWorldMatrix, float4(localV,1));
					v.pos = UnityWorldToClipPos(worldPos);
					int normalIndex = i / 6;
					v.normal = normalize(mul(cubeNormals[normalIndex], (float3x3)_WorldObjMatrix));
					outStream.Append(v);

					if ((i + 1) % 3 == 0)
						outStream.RestartStrip();
				}
			}

			fixed4 frag(v2f i) : SV_Target
			{
			   clip(i.col.a - 0.1);
				half nl = max(0.75, dot(i.normal, _WorldSpaceLightPos0.xyz) + 0.25);
			   return lerp(_InitColor, i.col * nl, saturate((_Time.y - _InitTime) / _InitColorSpeed));
			}

			ENDCG
		}
	}
}
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

			struct v2g
			{
				float4 pos : SV_POSITION;
				float4 col : COLOR;
			};

			v2g vert(uint id : SV_VertexID)
			{
				v2g o;
				o.pos = float4(_Voxels[id].pos, 1);
				const float2 uvs = _Voxels[id].uv;
				o.col = tex2Dlod(_PixelatedTexture, float4(uvs.x, uvs.y, 0, 0));
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

			struct g2f
			{
				float4 pos : SV_POSITION;
				float4 col : COLOR;
				float3 normal : NORMAL;
			};

			[maxvertexcount(CUBE_VERTS)]
			void geom(point v2g input[1], uint pid : SV_PrimitiveID, inout TriangleStream<g2f> outStream)
			{
				if (input[0].col.a <= 0)
					return;

				g2f v;
				v.col = input[0].col;
				const float offset = _VoxelSize * 0.5;
				const float3 center = input[0].pos;

				for (uint i = 0; i < CUBE_VERTS; ++i)
				{
					const int normalIndex = i / 6;
					const float3 cubeV = cubeVertices[i];
					const float3 localV = center + cubeV * offset;
					const float4 worldPos = mul(_ObjWorldMatrix, float4(localV,1));

					v.pos = UnityWorldToClipPos(worldPos);
					v.normal = normalize(mul(cubeNormals[normalIndex], (float3x3)_WorldObjMatrix));
					outStream.Append(v);

					if ((i + 1) % 3 == 0)
						outStream.RestartStrip();
				}
			}

			fixed4 frag(g2f i) : SV_Target
			{
				half nl = max(0.75, dot(i.normal, _WorldSpaceLightPos0.xyz) + 0.25);	// Just some basic lighting so the voxel doesn't look plain
				return lerp(_InitColor, i.col * nl, saturate((_Time.y - _InitTime) / _InitColorSpeed));
			}

			ENDCG
		}
	}
}
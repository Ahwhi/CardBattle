Shader "UI/SparkleEffect"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        // Diagonal sweep highlight
        _SweepColor    ("Sweep Color",        Color)        = (1, 1, 0.85, 0.9)
        _SweepWidth    ("Sweep Width",        Range(0.01, 0.5))  = 0.12
        _SweepInterval ("Sweep Interval",     Range(1.0, 12.0)) = 4.0
        _SweepTilt     ("Sweep Tilt",         Range(0.0, 1.0))   = 0.45

        // Cross-star sparkles
        _SparkleColor     ("Sparkle Color",    Color)            = (1, 0.97, 0.8, 1)
        _SparkleIntensity ("Sparkle Intensity",Range(0.0, 2.0))  = 1.0
        _SparkleSpeed     ("Sparkle Speed",    Range(0.5, 10.0)) = 3.0
        _SparkleScale     ("Sparkle Density",  Range(2.0, 20.0)) = 7.0
        _SparkleSize      ("Sparkle Size",     Range(0.01, 0.4)) = 0.12

        // Required by Unity UI system
        [HideInInspector] _StencilComp     ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil         ("Stencil ID",         Float) = 0
        [HideInInspector] _StencilOp       ("Stencil Operation",  Float) = 0
        [HideInInspector] _StencilWriteMask("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask",  Float) = 255
        [HideInInspector] _ColorMask       ("Color Mask",         Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"             = "Transparent"
            "IgnoreProjector"   = "True"
            "RenderType"        = "Transparent"
            "PreviewType"       = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref       [_Stencil]
            Comp      [_StencilComp]
            Pass      [_StencilOp]
            ReadMask  [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull     Off
        Lighting Off
        ZWrite   Off
        ZTest    [unity_GUIZTestMode]
        Blend    SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target   3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4    _MainTex_ST;
            fixed4    _Color;

            fixed4 _SweepColor;
            half   _SweepWidth;
            half   _SweepInterval;
            half   _SweepTilt;

            fixed4 _SparkleColor;
            half   _SparkleIntensity;
            half   _SparkleSpeed;
            half   _SparkleScale;
            half   _SparkleSize;

            float4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex   = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color    = v.color * _Color;
                return OUT;
            }

            // Pseudo-random hash: maps a 2D coordinate to [0, 1)
            half Hash21(half2 p)
            {
                p = frac(p * half2(127.1h, 311.7h));
                p += dot(p, p + 74.27h);
                return frac(p.x * p.y);
            }

            // 4-pointed cross-star shape centered at origin.
            // Each arm extends along the x or y axis and fades quickly in the perpendicular direction.
            half CrossStar(half2 d, half size)
            {
                half hArm = size / (abs(d.x) + abs(d.y) * 5.0h + 0.002h);
                half vArm = size / (abs(d.y) + abs(d.x) * 5.0h + 0.002h);
                return saturate(max(hArm, vArm) * 1.2h - 0.2h);
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 color = tex2D(_MainTex, IN.texcoord) * IN.color;
                half   alpha = color.a;

                // ----------------------------------------------------------
                // Sweep: a bright diagonal band that glides across the image
                // ----------------------------------------------------------
                half t         = frac(_Time.y / max(_SweepInterval, 0.01h));
                half sweepPos  = t * (1.0h + _SweepWidth * 2.0h) - _SweepWidth;

                // Diagonal projection: mix x and y by tilt amount
                half uvProj    = (IN.texcoord.x + IN.texcoord.y * _SweepTilt)
                                 / (1.0h + _SweepTilt);
                half sweepDist = abs(uvProj - sweepPos);
                half sweep     = 1.0h - saturate(sweepDist / max(_SweepWidth, 0.001h));
                sweep = sweep * sweep * sweep;          // smooth cubic falloff
                sweep *= _SweepColor.a * alpha;

                color.rgb += _SweepColor.rgb * sweep;

                // ----------------------------------------------------------
                // Sparkles: twinkling 4-pointed stars on a UV grid
                // 2x2 neighbor check (4 iterations) keeps this mobile-safe.
                // ----------------------------------------------------------
                half2 gridUV = IN.texcoord * _SparkleScale;
                half2 cellID = floor(gridUV);
                half2 cellUV = frac(gridUV);

                half sparkleSum = 0.0h;

                // Check current cell and three neighbors (up, right, up-right).
                // Each neighbor nID contributes a sparkle at a hashed center inside that cell.
                UNITY_UNROLL
                for (int xi = 0; xi <= 1; xi++)
                {
                    UNITY_UNROLL
                    for (int yi = 0; yi <= 1; yi++)
                    {
                        half2 offset = half2(xi, yi);
                        half2 nID    = cellID - offset;

                        // Sparkle center inside neighbor cell: bias toward [0.2, 0.8]
                        // to reduce clipping at cell edges.
                        half2 center;
                        center.x = Hash21(nID + half2(0.37h, 0.11h)) * 0.6h + 0.2h;
                        center.y = Hash21(nID + half2(0.73h, 0.59h)) * 0.6h + 0.2h;

                        // Vector from current UV to this sparkle's center (in grid space)
                        half2 toSpark = cellUV + offset - center;

                        // Per-sparkle flicker: random phase and speed
                        half h       = Hash21(nID);
                        half phase   = h * 6.283185h;
                        half speed   = _SparkleSpeed * (0.5h + h);
                        half flicker = sin(_Time.y * speed + phase) * 0.5h + 0.5h;
                        flicker = flicker * flicker * flicker * flicker;   // sharpen pulse

                        sparkleSum += CrossStar(toSpark, _SparkleSize) * flicker;
                    }
                }

                sparkleSum = saturate(sparkleSum * _SparkleIntensity);
                // Sparkles only appear where the texture has opacity
                sparkleSum *= alpha;
                color.rgb  += _SparkleColor.rgb * sparkleSum;

                // ----------------------------------------------------------
                // UI masking (Mask component / RectMask2D support)
                // ----------------------------------------------------------
                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}

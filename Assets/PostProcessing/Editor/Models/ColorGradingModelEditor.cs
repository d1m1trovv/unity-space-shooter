using UnityEngine;
using UnityEngine.PostProcessing;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace UnityEditor.PostProcessing
{
    using Settings = ColorGradingModel.Settings;
    using Tonemapper = ColorGradingModel.Tonemapper;
	using ColorWheelMode = ColorGradingModel.ColorWheelMode;

    [PostProcessingModelEditor(typeof(ColorGradingModel))]
    public class ColorGradingModelEditor : PostProcessingModelEditor
    {
        static GUIContent[] s_Tonemappers =
        {
            new GUIContent("None"),
            new GUIContent("Filmic (ACES)"),
            new GUIContent("Neutral")
        };

        struct TonemappingSettings
        {
            public SerializedProperty tonemapper;
            public SerializedProperty neutralBlackIn;
            public SerializedProperty neutralWhiteIn;
            public SerializedProperty neutralBlackOut;
            public SerializedProperty neutralWhiteOut;
            public SerializedProperty neutralWhiteLevel;
            public SerializedProperty neutralWhiteClip;
        }

        struct BasicSettings
        {
            public SerializedProperty exposure;
            public SerializedProperty temperature;
            public SerializedProperty tint;
            public SerializedProperty hueShift;
            public SerializedProperty saturation;
            public SerializedProperty contrast;
        }

        struct ChannelMixerSettings
        {
            public SerializedProperty[] channels;
            public SerializedProperty currentEditingChannel;
        }

        struct ColorWheelsSettings
        {
	        public SerializedProperty mode;
            public SerializedProperty log;
            public SerializedProperty linear;
        }

        static GUIContent[] s_Curves =
        {
            new GUIContent("YRGB"),
            new GUIContent("Hue VS Hue"),
            new GUIContent("Hue VS Sat"),
            new GUIContent("Sat VS Sat"),
            new GUIContent("Lum VS Sat")
        };

        struct CurvesSettings
        {
            public SerializedProperty master;
            public SerializedProperty red;
            public SerializedProperty green;
            public SerializedProperty blue;

            public SerializedProperty hueVShue;
            public SerializedProperty hueVSsat;
            public SerializedProperty satVSsat;
            public SerializedProperty lumVSsat;

            public SerializedProperty currentEditingCurve;
            public SerializedProperty curveY;
            public SerializedProperty curveR;
            public SerializedProperty curveG;
            public SerializedProperty curveB;
        }

        TonemappingSettings m_Tonemapping;
        BasicSettings m_Basic;
        ChannelMixerSettings m_ChannelMixer;
        ColorWheelsSettings m_ColorWheels;
        CurvesSettings m_Curves;

        CurveEditor m_CurveEditor;
        Dictionary<SerializedProperty, Color> m_CurveDict;

		// Neutral tonemapping curve helper
        const int k_CurveResolution = 24;
        const float k_NeutralRangeX = 2f;
        const float k_NeutralRangeY = 1f;
        Vector3[] m_RectVertices = new Vector3[4];
        Vector3[] m_LineVertices = new Vector3[2];
        Vector3[] m_CurveVertices = new Vector3[k_CurveResolution];
	    Rect m_NeutralCurveRect;

        public override void OnEnable()
        {
            // Tonemapping settings
            m_Tonemapping = new TonemappingSettings
            {
                tonemapper = FindSetting((Settings x) => x.tonemapping.tonemapper),
                neutralBlackIn = FindSetting((Settings x) => x.tonemapping.neutralBlackIn),
                neutralWhiteIn = FindSetting((Settings x) => x.tonemapping.neutralWhiteIn),
                neutralBlackOut = FindSetting((Settings x) => x.tonemapping.neutralBlackOut),
                neutralWhiteOut = FindSetting((Settings x) => x.tonemapping.neutralWhiteOut),
                neutralWhiteLevel = FindSetting((Settings x) => x.tonemapping.neutralWhiteLevel),
                neutralWhiteClip = FindSetting((Settings x) => x.tonemapping.neutralWhiteClip)
            };

            // Basic settings
            m_Basic = new BasicSettings
            {
                exposure = FindSetting((Settings x) => x.basic.postExposure),
                temperature = FindSetting((Settings x) => x.basic.temperature),
                tint = FindSetting((Settings x) => x.basic.tint),
                hueShift = FindSetting((Settings x) => x.basic.hueShift),
                saturation = FindSetting((Settings x) => x.basic.saturation),
                contrast = FindSetting((Settings x) => x.basic.contrast)
            };

            // Channel mixer
            m_ChannelMixer = new ChannelMixerSettings
            {
                channels = new[]
                {
                    FindSetting((Settings x) => x.channelMixer.red),
                    FindSetting((Settings x) => x.channelMixer.green),
                    FindSetting((Settings x) => x.channelMixer.blue)
                },
                currentEditingChannel = FindSetting((Settings x) => x.channelMixer.currentEditingChannel)
            };

            // Color wheels
            m_ColorWheels = new ColorWheelsSettings
            {
				mode = FindSetting((Settings x) => x.colorWheels.mode),
                log = FindSetting((Settings x) => x.colorWheels.log),
                linear = FindSetting((Settings x) => x.colorWheels.linear)
            };

            // Curves
            m_Curves = new CurvesSettings
            {
                master = FindSetting((Settings x) => x.curves.master.curve),
                red = FindSetting((Settings x) => x.curves.red.curve),
                green = FindSetting((Settings x) => x.curves.green.curve),
                blue = FindSetting((Settings x) => x.curves.blue.curve),

                hueVShue = FindSetting((Settings x) => x.curves.hueVShue.curve),
                hueVSsat = FindSetting((Settings x) => x.curves.hueVSsat.curve),
                satVSsat = FindSetting((Settings x) => x.curves.satVSsat.curve),
                lumVSsat = FindSetting((Settings x) => x.curves.lumVSsat.curve),

                currentEditingCurve = FindSetting((Settings x) => x.curves.e_CurrentEditingCurve),
                curveY = FindSetting((Settings x) => x.curves.e_CurveY),
                curveR = FindSetting((Settings x) => x.curves.e_CurveR),
                curveG = FindSetting((Settings x) => x.curves.e_CurveG),
                curveB = FindSetting((Settings x) => x.curves.e_CurveB)
            };

            // Prepare the curve editor and extract curve display settings
            m_CurveDict = new Dictionary<SerializedProperty, Color>();

            var settings = CurveEditor.Settings.defaultSettings;

            m_CurveEditor = new CurveEditor(settings);
            AddCurve(m_Curves.master,   new Color(1f, 1f, 1f), 2, false);
            AddCurve(m_Curves.red,      new Color(1f, 0f, 0f), 2, false);
            AddCurve(m_Curves.green,    new Color(0f, 1f, 0f), 2, false);
            AddCurve(m_Curves.blue,     new Color(0f, 0.5f, 1f), 2, false);
            AddCurve(m_Curves.hueVShue, new Color(1f, 1f, 1f), 0, true);
            AddCurve(m_Curves.hueVSsat, new Color(1f, 1f, 1f), 0, true);
            AddCurve(m_Curves.satVSsat, new Color(1f, 1f, 1f), 0, false);
            AddCurve(m_Curves.lumVSsat, new Color(1f, 1f, 1f), 0, false);
        }

        void AddCurve(SerializedProperty prop, Color color, uint minPointCount, bool loop)
        {
            var state = CurveEditor.CurveState.defaultState;
            state.color = color;
            state.visible = false;
            state.minPointCount = minPointCount;
            state.onlyShowHandlesOnSelection = true;
            state.zeroKeyConstantValue = 0.5f;
            state.loopInBounds = loop;
            m_CurveEditor.Add(prop, state);
            m_CurveDict.Add(prop, color);
        }

        public override void OnDisable()
        {
            m_CurveEditor.RemoveAll();
        }

        public override void OnInspectorGUI()
        {
            DoGUIFor("Tonemapping", DoTonemappingGUI);
            EditorGUILayout.Space();
            DoGUIFor("Basic", DoBasicGUI);
            EditorGUILayout.Space();
            DoGUIFor("Channel Mixer", DoChannelMixerGUI);
            EditorGUILayout.Space();
            DoGUIFor("Trackballs", DoColorWheelsGUI);
            EditorGUILayout.Space();
            DoGUIFor("Grading Curves", DoCurvesGUI);
        }

        void DoGUIFor(string title, Action func)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            func();
            EditorGUI.indentLevel--;
        }

        void DoTonemappingGUI()
        {
            int tid = EditorGUILayout.Popup(EditorGUIHelper.GetContent("Tonemapper"), m_Tonemapping.tonemapper.intValue, s_Tonemappers);

            if (tid == (int)Tonemapper.Neutral)
            {
	            DrawNeutralTonemappingCurve();

                EditorGUILayout.PropertyField(m_Tonemapping.neutralBlackIn, EditorGUIHelper.GetContent("Black In"));
                EditorGUILayout.PropertyField(m_Tonemapping.neutralWhiteIn, EditorGUIHelper.GetContent("White In"));
                EditorGUILayout.PropertyField(m_Tonemapping.neutralBlackOut, EditorGUIHelper.GetContent("Black Out"));
                EditorGUILayout.PropertyField(m_Tonemapping.neutralWhiteOut, EditorGUIHelper.GetContent("White Out"));
                EditorGUILayout.PropertyField(m_Tonemapping.neutralWhiteLevel, EditorGUIHelper.GetContent("White Level"));
                EditorGUILayout.PropertyField(m_Tonemapping.neutralWhiteClip, EditorGUIHelper.GetContent("White Clip"));
            }

            m_Tonemapping.tonemapper.intValue = tid;
        }

	    void DrawNeutralTonemappingCurve()
	    {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(EditorGUI.indentLevel * 15f);
                m_NeutralCurveRect = GUILayoutUtility.GetRect(128, 80);
            }

			// Background
			m_RectVertices[0] = PointInRect(             0f,              0f);
            m_RectVertices[1] = PointInRect(k_NeutralRangeX,              0f);
            m_RectVertices[2] = PointInRect(k_NeutralRangeX, k_NeutralRangeY);
            m_RectVertices[3] = PointInRect(             0f, k_NeutralRangeY);

            Handles.DrawSolidRectangleWithOutline(
                m_RectVertices,
                Color.white * 0.1f,
                Color.white * 0.4f
                );

            // Horizontal lines
            for (var i = 1; i < k_NeutralRangeY; i++)
                DrawLine(0, i, k_NeutralRangeX, i, 0.4f);

            // Vertical lines
            for (var i = 1; i < k_NeutralRangeX; i++)
                DrawLine(i, 0, i, k_NeutralRangeY, 0.4f);

			// Label
            Handles.Label(
                PointInRect(0, k_NeutralRangeY) + Vector3.right,
                "Neutral Tonemapper", EditorStyles.miniLabel
                );

			// Precompute some values
            var tonemap = ((ColorGradingModel)target).settings.tonemapping;

		    const float scaleFactor = 20f;
            const float scaleFactorHalf = scaleFactor * 0.5f;

            float inBlack = tonemap.neutralBlackIn * scaleFactor + 1f;
            float outBlack = tonemap.neutralBlackOut * scaleFactorHalf + 1f;
            float inWhite = tonemap.neutralWhiteIn / scaleFactor;
            float outWhite = 1f - tonemap.neutralWhiteOut / scaleFactor;
            float blackRatio = inBlack / outBlack;
            float whiteRatio = inWhite / outWhite;

            const float a = 0.2f;
            float b = Mathf.Max(0f, Mathf.LerpUnclamped(0.57f, 0.37f, blackRatio));
            float c = Mathf.LerpUnclamped(0.01f, 0.24f, whiteRatio);
            float d = Mathf.Max(0f, Mathf.LerpUnclamped(0.02f, 0.20f, blackRatio));
            const float e = 0.02f;
            const float f = 0.30f;
		    float whiteLevel = tonemap.neutralWhiteLevel;
		    float whiteClip = tonemap.neutralWhiteClip / scaleFactorHalf;

			// Tonemapping curve
            var vcount = 0;
            while (vcount < k_CurveResolution)
            {
                float x = k_NeutralRangeX * vcount / (k_CurveResolution - 1);
                float y = NeutralTonemap(x, a, b, c, d, e, f, whiteLevel, whiteClip);

                if (y < k_NeutralRangeY)
                {
                    m_CurveVertices[vcount++] = PointInRect(x, y);
                }
                else
                {
                    if (vcount > 1)
                    {
                        // Extend the last segment to the top edge of the rect.
                        var v1 = m_CurveVertices[vcount - 2];
                        var v2 = m_CurveVertices[vcount - 1];
                        var clip = (m_NeutralCurveRect.y - v1.y) / (v2.y - v1.y);
                        m_CurveVertices[vcount - 1] = v1 + (v2 - v1) * clip;
                    }
                    break;
                }
            }

            if (vcount > 1)
            {
                Handles.color = Color.white * 0.9f;
                Handles.DrawAAPolyLine(2.0f, vcount, m_CurveVertices);
            }
	    }

		void DrawLine(float x1, float y1, float x2, float y2, float grayscale)
        {
            m_LineVertices[0] = PointInRect(x1, y1);
            m_LineVertices[1] = PointInRect(x2, y2);
            Handles.color = Color.white * grayscale;
            Handles.DrawAAPolyLine(2f, m_LineVertices);
        }

		Vector3 PointInRect(float x, float y)
        {
            x = Mathf.Lerp(m_NeutralCurveRect.x, m_NeutralCurveRect.xMax, x / k_NeutralRangeX);
            y = Mathf.Lerp(m_NeutralCurveRect.yMax, m_NeutralCurveRect.y, y / k_NeutralRangeY);
            return new Vector3(x, y, 0);
        }

		float NeutralCurve(float x, float a, float b, float c, float d, float e, float f)
		{
			return ((x * (a * x + c * b) + d * e) / (x * (a * x + b) + d * f)) - e / f;
		}

	    float NeutralTonemap(float x, float a, float b, float c, float d, float e, float f, float whiteLevel, float whiteClip)
	    {
			x = Mathf.Max(0f, x);

			// Tonemap
			float whiteScale = 1f / NeutralCurve(whiteLevel, a, b, c, d, e, f);
			x = NeutralCurve(x * whiteScale, a, b, c, d, e, f);
			x *= whiteScale;

			// Post-curve white point adjustment
			x /= whiteClip;

			return x;
	    }

        void DoBasicGUI()
        {
            EditorGUILayout.PropertyField(m_Basic.exposure, EditorGUIHelper.GetContent("Post Exposure (EV)"));
            EditorGUILayout.PropertyField(m_Basic.temperature);
            EditorGUILayout.PropertyField(m_Basic.tint);
            EditorGUILayout.PropertyField(m_Basic.hueShift);
            EditorGUILayout.PropertyField(m_Basic.saturation);
            EditorGUILayout.PropertyField(m_Basic.contrast);
        }

        void DoChannelMixerGUI()
        {
            int currentChannel = m_ChannelMixer.currentEditingChannel.intValue;

            EditorGUI.BeginChangeCheck();
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PrefixLabel("Channel");
                    if (GUILayout.Toggle(currentChannel == 0, EditorGUIHelper.GetContent("Red|Red output channel."), EditorStyles.miniButtonLeft)) currentChannel = 0;
                    if (GUILayout.Toggle(currentChannel == 1, EditorGUIHelper.GetContent("Green|Green output channel."), EditorStyles.miniButtonMid)) currentChannel = 1;
                    if (GUILayout.Toggle(currentChannel == 2, EditorGUIHelper.GetContent("Blue|Blue output channel."), EditorStyles.miniButtonRight)) currentChannel = 2;
                }
            }
            if (EditorGUI.EndChangeCheck())
            {
                GUI.FocusControl(null);
            }

            var serializedChannel = m_ChannelMixer.channels[currentChannel];
            m_ChannelMixer.currentEditingChannel.intValue = currentChannel;

            var v = serializedChannel.vector3Value;
            v.x = EditorGUILayout.Slider(EditorGUIHelper.GetContent("Red|Modify influence of the red channel within the overall mix."), v.x, -2f, 2f);
            v.y = EditorGUILayout.Slider(EditorGUIHelper.GetContent("Green|Modify influence of the green channel within the overall mix."), v.y, -2f, 2f);
            v.z = EditorGUILayout.Slider(EditorGUIHelper.GetContent("Blue|Modify influence of the blue channel within the overall mix."), v.z, -2f, 2f);
            serializedChannel.vector3Value = v;
        }

        void DoColorWheelsGUI()
        {
	        int wheelMode = m_ColorWheels.mode.intValue;

	        using (new EditorGUILayout.HorizontalScope())
	        {
		        GUILayout.Space(15);
		        if (GUILayout.Toggle(wheelMode == (int)ColorWheelMode.Linear, "Linear", EditorStyles.miniButtonLeft)) wheelMode = (int)ColorWheelMode.Linear;
		        if (GUILayout.Toggle(wheelMode == (int)ColorWheelMode.Log, "Log", EditorStyles.miniButtonRight)) wheelMode = (int)ColorWheelMode.Log;
	        }

	        m_ColorWheels.mode.intValue = wheelMode;
	        EditorGUILayout.Space();

	        if (wheelMode == (int)ColorWheelMode.Linear)
	        {
		        EditorGUILayout.PropertyField(m_ColorWheels.linear);
		        WheelSetTitle(GUILayoutUtility.GetLastRect(), "Linear Controls");
	        }
			else if (wheelMode == (int)ColorWheelMode.Log)
			{
				EditorGUILayout.PropertyField(m_ColorWheels.log);
				WheelSetTitle(GUILayoutUtility.GetLastRect(), "Log Controls");
			}
        }

        static void WheelSetTitle(Rect position, string label)
        {
            var matrix = GUI.matrix;
            var rect = new Rect(position.x - 10f, position.y, TrackballGroupDrawer.m_Size, TrackballGroupDrawer.m_Size);
            GUIUtility.RotateAroundPivot(-90f, rect.center);
            GUI.Label(rect, label, FxStyles.centeredMiniLabel);
            GUI.matrix = matrix;
        }

        void ResetVisibleCurves()
        {
            foreach (var curve in m_CurveDict)
            {
                var state = m_CurveEditor.GetCurveState(curve.Key);
                state.visible = false;
                m_CurveEditor.SetCurveState(curve.Key, state);
            }
        }

        void SetCurveVisible(SerializedProperty prop)
        {
            var state = m_CurveEditor.GetCurveState(prop);
            state.visible = true;
            m_CurveEditor.SetCurveState(prop, state);
        }

        bool SpecialToggle(bool value, string name, out bool rightClicked)
        {
            var rect = GUILayoutUtility.GetRect(EditorGUIHelper.GetContent(name), EditorStyles.toolbarButton);

            var e = Event.current;
            rightClicked = (e.type == EventType.MouseUp && rect.Contains(e.mousePosition) && e.button == 1);

            return GUI.Toggle(rect, value, name, EditorStyles.toolbarButton);
        }

        static Material s_MaterialSpline;

        void DoCurvesGUI()
        {
            EditorGUILayout.Space();
            EditorGUI.indentLevel -= 2;
            ResetVisibleCurves();

            using (new EditorGUI.DisabledGroupScope(serializedProperty.serializedObject.isEditingMultipleObjects))
            {
                int curveEditingId = 0;

                // Top toolbar
                using (new GUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    curveEditingId = EditorGUILayout.Popup(m_Curves.currentEditingCurve.intValue, s_Curves, EditorStyles.toolbarPopup, GUILayout.MaxWidth(150f));
                    bool y = false, r = false, g = false, b = false;

                    if (curveEditingId == 0)
                    {
                        EditorGUILayout.Space();

                        bool rightClickedY, rightClickedR, rightClickedG, rightClickedB;

      ÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿgÿgÿ‰ÿƒÿƒÿƒÿ‰ÿƒÿƒÿƒÿ‰ÿƒÿƒÿƒÿ‰ÿƒÿƒÿƒÿ‰ÿƒÿƒÿƒÿ‰ÿƒÿƒÿƒÿ‰ÿƒÿƒÿƒÿ‰ÿƒÿƒÿƒÿ‰ÿƒÿƒÿƒÿ‰ÿƒÿƒÿƒÿ‰ÿƒÿƒÿƒÿ‰ÿƒÿƒÿƒÿ‰ÿƒÿƒÿƒÿ‰ÿƒÿƒÿƒÿ‰ÿƒÿƒÿƒÿ‰ÿƒÿƒÿƒÿ‰ÿƒÿƒÿƒÿ‰ÿƒÿƒÿƒÿ‰ÿƒÿƒÿƒÿ‰ÿƒÿƒÿƒÿ‰ÿƒÿƒÿƒÿ‰ÿƒÿƒÿƒÿ‰ÿƒÿƒÿƒÿ‰ÿƒÿƒÿƒÿ‰ÿƒÿƒÿƒÿ‰ÿƒÿƒÿƒÿ‰ÿƒÿƒÿƒÿ‰ÿƒÿƒÿƒÿ‰ÿƒÿƒÿƒÿ‰ÿƒÿƒÿƒÿ‰ÿƒÿƒÿƒÿ‰ÿƒÿƒÿƒÿ‰ÿƒÿƒÿƒÿ‰ÿƒÿƒÿƒÿ‰ÿƒÿƒÿƒÿ‰ÿƒÿƒÿƒÿ‰ÿƒÿƒÿƒÿ‰ÿƒÿƒÿƒÿ‰ÿƒÿƒÿƒÿ‰ÿƒÿƒÿƒÿ‰ÿuÿvÿuÿ|ÿuÿ|ÿuÿ ÿuÿ|ÿuÿ ÿuÿ|ÿuÿ ÿ ÿ ÿ ÿ ÿ ÿ ÿ ÿ§ÿ ÿ ÿ ÿ§ÿ ÿ ÿ ÿ ÿ ÿ ÿ|ÿ§ÿ ÿ ÿ ÿ§ÿ ÿ ÿ ÿ§ÿ ÿ ÿ ÿ§ÿ ÿ ÿ ÿ§ÿ ÿ ÿ ÿ§ÿ ÿ ÿ ÿ§ÿ ÿ ÿ ÿ§ÿ ÿ ÿ ÿ§ÿ ÿ ÿ ÿ§ÿ ÿ ÿ ÿ§ÿ ÿ ÿ ÿ§ÿ ÿ ÿ ÿ§ÿ ÿ ÿ ÿ§ÿ ÿ ÿ ÿ§ÿ ÿ ÿ|ÿ ÿ ÿ ÿ ÿ§ÿ ÿ ÿ ÿ§ÿ ÿ ÿ ÿ§ÿ ÿ ÿ ÿ§ÿuÿ|ÿuÿ ÿuÿ|ÿuÿ ÿuÿ|ÿuÿ ÿuÿ|ÿuÿ ÿƒÿƒÿƒÿ‰ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿ3ÿ9ÿÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿ9ÿÿ ÿ ÿ ÿ ÿ ÿ ÿ ÿ ÿ ÿ ÿ ÿ ÿ ÿ ÿ ÿ ÿ ÿ ^ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ9ÿ9ÿ3ÿ9ÿ9ÿ9ÿ3ÿ9ÿ9ÿ9ÿ3ÿ9ÿ9ÿ9ÿ3ÿ9ÿ9ÿ9ÿ3ÿ9ÿ9ÿ9ÿ3ÿ9ÿ9ÿ9ÿ3ÿ9ÿ9ÿ9ÿ3ÿ9ÿ9ÿ9ÿ3ÿ9ÿ9ÿ9ÿ3ÿ9ÿ9ÿ9ÿ3ÿ9ÿ9ÿ9ÿ3ÿ9ÿ9ÿ9ÿ3ÿ9ÿ9ÿ9ÿ3ÿ9ÿ9ÿ9ÿ3ÿ9ÿ9ÿ9ÿ3ÿ9ÿ9ÿ9ÿ3ÿ9ÿ9ÿ9ÿ3ÿ9ÿ9ÿ9ÿ3ÿ9ÿ9ÿ9ÿ3ÿ9ÿ9ÿ9ÿ3ÿ9ÿ9ÿ9ÿ3ÿ9ÿ9ÿ9ÿ3ÿ9ÿ9ÿ9ÿ3ÿ9ÿ9ÿ^ÿ9ÿ^ÿ9ÿ^ÿ9ÿ^ÿ9ÿ^ÿ9ÿ^ÿ9ÿ^ÿ9ÿ^ÿ9ÿ^ÿ9ÿ^ÿ9ÿ^ÿ9ÿ^ÿ9ÿ^ÿ9ÿ^ÿ9ÿ^ÿ9ÿ^ÿ9ÿ^ÿ9ÿ^ÿ9ÿ^ÿ9ÿ^ÿ9ÿ^ÿ9ÿ^ÿ9ÿ^ÿ9ÿ^ÿ9ÿ^ÿ9ÿ^ÿ9ÿ^ÿ9ÿ^ÿ9ÿ^ÿ9ÿ^ÿ9ÿ^ÿ9ÿ^ÿ9ÿ^ÿ9ÿ^ÿ9ÿ^ÿ9ÿ^ÿ9ÿ^ÿ9ÿ^ÿ9ÿ^ÿ9ÿ^ÿ9ÿ^ÿ9ÿ^ÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿÈÿÍÿÇÿÍÿÇÿÍÿÇÿÍÿÇÿÍÿÇÿÌÿÇÿÍÿÇÿÌÿÇÿÍÿÇÿÌÿÇÿÍÿÇÿÌÿÇÿÍÿÇÿÌÿÇÿÍÿÇÿÌÿÇÿÇÿÇÿÇÿÇÿÇÿÆÿÇÿÀÿÇÿÿÿÿÿÿÿ®ÿƒÿ¨ÿƒÿ®ÿƒÿ¨ÿƒÿ®ÿƒÿ¨ÿƒÿ®ÿƒÿ¨ÿƒÿÿÿÿÿÿÿÈÿÇÿÆÿÇÿÇÿÇÿÇÿÇÿÇÿÇÿÇÿÇÿÀÿÇÿÆÿÇÿÀÿÎÿÿÿÿÿÿÿÿÿÈÿÇÿÀÿÇÿÆÿÇÿÇÿÇÿÇÿÇÿÇÿÍÿÌÿÌÿÌÿÇÿÇÿÿÿÿÿÿÿ¨ÿ¨ÿƒÿ®ÿƒÿ¨ÿƒÿ®ÿ¨ÿÿÿ¨ÿÿÿ¨ÿÿÿ¨ÿÿÿÿÿÿÿÿÿÿÿÆÿÇÿÀÿÇÿÇÿÇÿÇÿÍÿÇÿÌÿÇÿÍÿÇÿÌÿÇÿÍÿÇÿÌÿÇÿÍÿÇÿÌÿÇÿÍÿÇÿÌÿÇÿÍÿÇÿÌÿÇÿÍÿÇÿÍÿÇÿÍÿÇÿÍÿÇÿÍÿÎÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿ¨ÿÿÿ¨ÿÿÿ¨ÿÿÿ¨ÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ¨ÿÿÿ¨ÿÿÿ¨ÿÿÿ¨ÿÿÿ¨ÿÿÿ¨ÿÿÿ¨ÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿ¨ÿÿÿ¨ÿÿÿ¨ÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿ¯ÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿ`ÿgÿ<ÿgÿ`ÿgÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿƒÿ^ÿvÿuÿuÿuÿvÿuÿvÿuÿvÿuÿvÿuÿvÿuÿvÿuÿ ÿ|ÿ ÿuÿ ÿ|ÿ ÿ|ÿ ÿ ÿ ÿ|ÿ ÿ ÿ ÿuÿ ÿvÿ ÿ|ÿ ÿ ÿ ÿ|ÿ ÿ ÿ ÿ|ÿ ÿ ÿ ÿ|ÿ ÿ ÿ ÿ|ÿ ÿ ÿ ÿ|ÿ ÿ ÿ ÿ|ÿ ÿ ÿ ÿ|ÿ ÿ ÿ ÿ|ÿ ÿ ÿ ÿ|ÿ ÿ ÿ ÿ|ÿ ÿ ÿ ÿ|ÿ ÿ ÿ ÿ|ÿ ÿ ÿ ÿ|ÿ ÿ ÿ ÿ|ÿ ÿvÿ ÿuÿ ÿ ÿ ÿ|ÿ ÿ ÿ ÿ|ÿ ÿ ÿ ÿ|ÿ ÿ ÿ ÿ|ÿvÿuÿvÿuÿvÿuÿvÿuÿvÿuÿvÿuÿvÿuÿvÿuÿƒÿ^ÿƒÿ^ÿƒÿƒÿ¨ÿƒÿ®ÿƒÿ¨ÿƒÿ®ÿƒÿ¨ÿƒÿ®ÿƒÿ¨ÿƒÿ®ÿƒÿ¨ÿƒÿ®ÿƒÿ¨ÿƒÿ®ÿƒÿ¨ÿƒÿ®ÿƒÿ¨ÿƒÿ®ÿƒÿ¨ÿƒÿ®ÿƒÿ¨ÿƒÿ®ÿƒÿ¨ÿƒÿ®ÿƒÿ¨ÿƒÿ®ÿƒÿ¨ÿƒÿ®ÿƒÿ¨ÿƒÿ®ÿƒÿ¨ÿƒÿ3ÿÿ3ÿÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿ9ÿ3ÿÿ ÿ ÿ ÿ ÿ ÿ ÿ ÿ ÿ ÿ ÿ ÿ ÿ ÿ ÿ ÿ ÿ ®ÿ9ÿ3ÿ9ÿ9ÿ9ÿ3ÿ9ÿ9ÿ9ÿ3ÿ9ÿ9ÿ9ÿ3ÿ9ÿ9ÿ9ÿ3ÿ9ÿ9ÿ9ÿ3ÿ9ÿ9ÿ9ÿ3ÿ9ÿ9ÿ9ÿ3ÿ9ÿ9ÿ9ÿ3ÿ9ÿ9ÿ9ÿ3ÿ^ÿ9ÿ9ÿ9ÿ^ÿ9ÿ9ÿ9ÿ^ÿ9ÿ9ÿ9ÿ^ÿ9ÿ9ÿ9ÿ^ÿ9ÿ9ÿ9ÿ^ÿ9ÿ9ÿ9ÿ^ÿ9ÿ9ÿ9ÿ^ÿ9ÿ9ÿ9ÿ^ÿ9ÿ9ÿ9ÿ^ÿ9ÿ9ÿ9ÿ^ÿ9ÿ9ÿ9ÿ^ÿ9ÿ9ÿ9ÿ^ÿ9ÿ9ÿ9ÿ^ÿ9ÿ9ÿ9ÿ^ÿ9ÿ9ÿ9ÿ^ÿ9ÿ9ÿ9ÿ^ÿ9ÿ9ÿ9ÿ^ÿ9ÿ9ÿ9ÿ^ÿ9ÿ9ÿ9ÿ^ÿ9ÿ9ÿ9ÿ^ÿ9ÿ9ÿ9ÿ^ÿ9ÿ9ÿ9ÿ^ÿ9ÿ9ÿ9ÿ^ÿ9ÿ9ÿ9ÿ^ÿ9ÿ^ÿ9ÿdÿ9ÿ^ÿ9ÿdÿ9ÿ^ÿ9ÿdÿ9ÿ^ÿ9ÿdÿ9ÿ^ÿ9ÿdÿ9ÿ^ÿ9ÿdÿ9ÿ^ÿ9ÿdÿ9ÿ^ÿ9ÿdÿ9ÿ^ÿ9ÿdÿ9ÿ^ÿ9ÿdÿ9ÿ^ÿ9ÿdÿ9ÿ^ÿ9ÿdÿ9ÿ^ÿ9ÿdÿ9ÿ^ÿ9ÿdÿ9ÿ^ÿ9ÿdÿ9ÿ^ÿ9ÿdÿ9ÿ^ÿ9ÿdÿ9ÿ^ÿ9ÿdÿ9ÿ^ÿ9ÿdÿ9ÿ^ÿ9ÿdÿ9ÿ^ÿ9ÿdÿƒÿƒÿ^ÿ‰ÿƒÿƒÿ^ÿ‰ÿÿÿÍÿÇÿÍÿÍÿÍÿÇÿÍÿÇÿÍÿÇÿÍÿÇÿÍÿÇÿÍÿÇÿÍÿÇÿÍÿÇÿÍÿÇÿÍÿÇÿÍÿÇÿÍÿÇÿÍÿÇÿÍÿÇÿÌÿÇÿÌÿÇÿÌÿÇÿÌÿÇÿÇÿÆÿÍÿÿÿÿÿ®ÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿÿÿÿÿÿÿÏÿÆÿÇÿÇÿÇÿÇÿÌÿÇÿÇÿÆÿÇÿÇÿÇÿÆÿÇÿÉÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÎÿÆÿÇÿÇÿÇÿÇÿÌÿÇÿÌÿÌÿÍÿÌÿÌÿÇÿÍÿÿÿÿÿÿÿÿÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿ®ÿÿÿ¨ÿÿÿÿÿÿÿ¨ÿÿÿÿÿÿÿÉÿÇÿÇÿÇÿÆÿÌÿÇÿÌÿÇÿÌÿÇÿÍÿÇÿÍÿÇÿÍÿÇÿÍÿÇÿÍÿÇÿÍÿÇÿÍÿÇÿÍÿÇÿÍÿÇÿÍÿÇÿÍÿÇÿÍÿÇÿÍÿÇÿÍÿÍÿÍÿÇÿÍÿÏÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿ¨ÿÿÿÿÿÿÿ¨ÿÿÿÿÿÿÿ¨ÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‰ÿƒÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿ¨ÿÿÿÿÿÿÿ¨ÿÿÿÿÿÿÿ¨ÿÿÿÿÿÿÿ¨ÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿ¨ÿÿÿÿÿÿÿ¨ÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿ‹ÿgÿgÿgÿgÿ`ÿgÿgÿgÿ`ÿgÿgÿƒÿ^ÿ‰ÿƒÿƒÿ^ÿ‰ÿƒÿƒÿ^ÿ‰ÿƒÿƒÿ^ÿ‰ÿƒÿƒÿ^ÿ‰ÿƒÿƒÿ^ÿ‰ÿƒÿƒÿ^ÿ‰ÿƒÿƒÿ^ÿ‰ÿƒÿƒÿ^ÿ‰ÿƒÿƒÿ^ÿ‰ÿƒÿƒÿ^ÿ‰ÿƒÿƒÿ^ÿ‰ÿƒÿƒÿ^ÿ‰ÿƒÿƒÿ^ÿ‰ÿƒÿƒÿ^ÿ‰ÿƒÿƒÿ^ÿ‰ÿƒÿƒÿ^ÿ‰ÿƒÿƒÿ^ÿ‰ÿƒÿƒÿ^ÿ‰ÿƒÿƒÿ^ÿ‰ÿƒÿƒÿ^ÿ‰ÿƒÿƒÿ^ÿ‰ÿƒÿƒÿ^ÿ‰ÿƒÿƒÿ^ÿ‰ÿƒÿƒÿ^ÿ‰ÿƒÿƒÿ^ÿ‰ÿƒÿƒÿ^ÿ‰ÿƒÿƒÿ^ÿ‰ÿƒÿƒÿ^ÿ‰ÿƒÿƒÿ^ÿ‰ÿƒÿƒÿ^ÿ‰ÿƒÿƒÿ^ÿ‰ÿƒÿƒÿ^ÿ‰ÿƒÿƒÿ^ÿ‰ÿƒÿƒÿ^ÿ‰ÿƒÿƒÿ^ÿ‰ÿƒÿƒÿ^ÿ‰ÿƒÿƒÿ^ÿ‰ÿƒÿƒÿ^ÿ‰ÿƒÿƒÿ^ÿ‰ÿƒÿƒÿuÿ|ÿuÿvÿuÿ|ÿuÿ|ÿuÿ ÿuÿ|ÿuÿ ÿuÿ|ÿ|ÿ ÿ ÿ ÿ|ÿ ÿ ÿ ÿ ÿ¡ÿ ÿ ÿ ÿ¡ÿ ÿ ÿ|ÿ ÿ ÿ ÿ ÿ¡ÿ ÿ ÿ ÿ¡ÿ ÿ ÿ ÿ¡ÿ ÿ ÿ ÿ¡ÿ ÿ ÿ ÿ¡ÿ ÿ ÿ ÿ¡ÿ ÿ ÿ ÿ¡ÿ ÿ ÿ ÿ¡ÿ ÿ ÿ ÿ¡ÿ ÿ ÿ ÿ¡ÿ ÿ ÿ ÿ¡ÿ ÿ ÿ ÿ¡ÿ ÿ ÿ ÿ¡ÿ ÿ ÿ ÿ¡ÿ ÿ ÿ ÿ ÿ ÿ ÿ|ÿ¡ÿ ÿ ÿ ÿ¡ÿ ÿ ÿ ÿ¡ÿ ÿ ÿ ÿ¡ÿ ÿ ÿuÿ ÿuÿ|ÿuÿ ÿuÿ|ÿuÿ ÿuÿ|ÿuÿ ÿuÿ|ÿ^ÿ‰ÿƒÿƒÿ^ÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿƒÿ®ÿÿ9ÿ3ÿ9ÿ3ÿ9ÿ9ÿ9ÿ3ÿ9ÿ9ÿ9ÿ3ÿ9ÿ9ÿ9ÿ3ÿ9ÿ9ÿ9ÿ3ÿ9ÿ9ÿ9ÿ3ÿ9ÿ9ÿ9ÿ3ÿ9ÿ9ÿ9ÿ3ÿ9ÿ9ÿ9ÿ3ÿ9ÿ9ÿ9ÿ3ÿ9ÿ9ÿ9ÿ3ÿ9ÿ9ÿ9ÿ3ÿ9ÿ9ÿ9ÿ3ÿ9ÿ9ÿ9ÿ3ÿ9ÿ9ÿ9ÿ3ÿ9ÿ9ÿ9ÿ3ÿ9ÿ9ÿ9ÿ3ÿ9ÿ
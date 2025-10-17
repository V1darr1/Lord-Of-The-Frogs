using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using System.IO;

namespace AkvatGames.NoiseGenerator
{
    public class NoiseGeneratorWindow : EditorWindow
    {
        private int textureWidth = 2048;
        private int textureHeight = 2048;
        private List<NoiseLayer> noiseLayers = new List<NoiseLayer>();

        private ReorderableList reorderableList;

        private bool isSquareTexture = true;
        private Color primaryColor = Color.white;
        private Color secondaryColor = Color.black;

        private int scale = 1;

        [MenuItem("Tools/Akvat Games/Noise Generator/Settings", priority = 1)]
        public static void ShowWindow()
        {
            GetWindow<NoiseGeneratorWindow>("Noise Generator");
        }

        [MenuItem("Tools/Akvat Games/Noise Generator/Delete Generated Textures", priority = 2)]
        public static void DeleteAllTextures()
        {
            string folderPath = "Assets/Akvat Games/Noise Generator/Generated Textures";

            if (Directory.Exists(folderPath))
            {
                string[] files = Directory.GetFiles(folderPath);

                foreach (var file in files)
                {
                    File.Delete(file);
                }

                AssetDatabase.Refresh();
                Debug.Log("All textures have been deleted from the GeneratedTextures folder.");
            }
            else
            {
                Debug.LogWarning("The directory does not exist.");
            }
        }

        private void OnEnable()
        {
            reorderableList = new ReorderableList(noiseLayers, typeof(NoiseLayer), true, true, true, true);

            reorderableList.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, "Noise Layers");
            };

            reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var element = reorderableList.list[index] as NoiseLayer;
                rect.y += 2;

                // Layer label and noise type selection
                EditorGUI.LabelField(new Rect(rect.x, rect.y, 70, EditorGUIUtility.singleLineHeight), "Layer " + (index + 1));
                element.noiseType = (NoiseLayer.NoiseType)EditorGUI.EnumPopup(new Rect(rect.x + 75, rect.y, 100, EditorGUIUtility.singleLineHeight), element.noiseType);

                // Scale field
                EditorGUI.LabelField(new Rect(rect.x + 180, rect.y, 50, EditorGUIUtility.singleLineHeight), "Scale");
                element.scale = EditorGUI.FloatField(new Rect(rect.x + 235, rect.y, 60, EditorGUIUtility.singleLineHeight), element.scale);

                // Intensity field
                EditorGUI.LabelField(new Rect(rect.x + 305, rect.y, 60, EditorGUIUtility.singleLineHeight), "Intensity");
                element.intensity = EditorGUI.FloatField(new Rect(rect.x + 370, rect.y, 60, EditorGUIUtility.singleLineHeight), element.intensity);
            };

            reorderableList.elementHeightCallback = (int index) =>
            {
                var element = reorderableList.list[index] as NoiseLayer;
                float baseHeight = EditorGUIUtility.singleLineHeight + 6; // Base height for basic fields
                if (element.overrideColors)
                {
                    return baseHeight + 3 * EditorGUIUtility.singleLineHeight + 15; // Add extra height for color fields
                }
                return baseHeight;
            };

            reorderableList.onAddCallback = (ReorderableList list) =>
            {
                noiseLayers.Add(new NoiseLayer());
            };

            reorderableList.onRemoveCallback = (ReorderableList list) =>
            {
                if (noiseLayers.Count > 0)
                {
                    noiseLayers.RemoveAt(list.index);
                }
            };
        }

        private void OnGUI()
        {
            GUILayout.Label("Noise Generator Settings", EditorStyles.boldLabel);

            isSquareTexture = EditorGUILayout.Toggle("Square Texture", isSquareTexture);

            if (isSquareTexture)
            {
                textureWidth = textureHeight = EditorGUILayout.IntField("Texture Size", textureWidth);
            }
            else
            {
                textureWidth = EditorGUILayout.IntField("Texture Width", textureWidth);
                textureHeight = EditorGUILayout.IntField("Texture Height", textureHeight);
            }

            primaryColor = EditorGUILayout.ColorField("Primary Color", primaryColor);
            secondaryColor = EditorGUILayout.ColorField("Secondary Color", secondaryColor);

            reorderableList.DoLayoutList();

            if (noiseLayers.Count == 0)
            {
                GUIStyle warningStyle = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.white },
                    alignment = TextAnchor.MiddleCenter,
                    padding = new RectOffset(10, 10, 5, 5),
                    margin = new RectOffset(0, 0, 10, 10),
                    fontSize = 12,
                    stretchWidth = true,
                    wordWrap = true
                };

                Rect warningRect = EditorGUILayout.BeginVertical();
                GUI.backgroundColor = Color.red;
                EditorGUILayout.HelpBox("Warning: No noise layers set.\nGenerating a texture will result in a solid primary color image.", MessageType.Warning);

                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("Generate Noise"))
            {
                GenerateNoiseTexture(textureWidth, textureHeight, scale, noiseLayers.ToArray(), primaryColor, secondaryColor);
            }

            GUI.enabled = noiseLayers.Count > 0;
            if (GUILayout.Button("Remove All Noise Layers"))
            {
                noiseLayers.Clear();
            }
            GUI.enabled = true;
        }

        private void GenerateNoiseTexture(int textureWidth, int textureHeight, int scale, NoiseLayer[] noiseLayers, Color primaryColor, Color secondaryColor)
        {
            Texture2D texture = new Texture2D(textureWidth, textureHeight);

            if (noiseLayers.Length == 0)
            {
                Color solidColor = primaryColor;
                for (int x = 0; x < texture.width; x++)
                {
                    for (int y = 0; y < texture.height; y++)
                    {
                        texture.SetPixel(x, y, solidColor);
                    }
                }
            }
            else
            {
                int scaledWidth = textureWidth / scale;
                int scaledHeight = textureHeight / scale;

                for (int x = 0; x < texture.width; x++)
                {
                    for (int y = 0; y < texture.height; y++)
                    {
                        float pixelValue = 0f;
                        Color layerPrimaryColor = primaryColor;
                        Color layerSecondaryColor = secondaryColor;

                        foreach (var layer in noiseLayers)
                        {
                            if (layer.overrideColors)
                            {
                                layerPrimaryColor = layer.overridePrimaryColor;
                                layerSecondaryColor = layer.overrideSecondaryColor;
                            }

                            float noiseValue = GetNoiseValue(layer, x, y, scaledWidth, scaledHeight);
                            pixelValue += noiseValue;
                        }

                        pixelValue = Mathf.Clamp01(pixelValue / noiseLayers.Length);

                        Color color = Color.Lerp(layerSecondaryColor, layerPrimaryColor, pixelValue);
                        texture.SetPixel(x, y, color);
                    }
                }
            }

            texture.Apply();

            string folderPath = "Assets/Akvat Games/Noise Generator/Generated Textures";
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string filePath = GetUniqueFilePath(folderPath, textureWidth, textureHeight, scale, noiseLayers, primaryColor, secondaryColor);
            byte[] bytes = texture.EncodeToPNG();
            File.WriteAllBytes(filePath, bytes);
            AssetDatabase.Refresh();
        }

        private string GetUniqueFilePath(string folderPath, int width, int height, int scale, NoiseLayer[] noiseLayers, Color primaryColor, Color secondaryColor)
        {
            string baseFileName = $"AkvatNoiseTexture";

            string extension = ".png";

            string filePath = System.IO.Path.Combine(folderPath, baseFileName + extension);
            int fileIndex = 1;

            while (System.IO.File.Exists(filePath))
            {
                filePath = System.IO.Path.Combine(folderPath, $"{baseFileName}_{fileIndex}{extension}");
                fileIndex++;
            }

            return filePath;
        }

        private float GetNoiseValue(NoiseLayer layer, int x, int y, int textureWidth, int textureHeight)
        {
            float noiseValue = 0f;

            float xCoord = (float)x / textureWidth;
            float yCoord = (float)y / textureHeight;

            xCoord *= layer.scale;
            yCoord *= layer.scale;

            switch (layer.noiseType)
            {
                case NoiseLayer.NoiseType.Perlin:
                    noiseValue = Mathf.PerlinNoise(xCoord, yCoord) * layer.intensity;
                    break;
            }

            return noiseValue;
        }
    }

    [System.Serializable]
    public class NoiseLayer
    {
        public enum NoiseType { Perlin, }
        public NoiseType noiseType;
        public float scale = 1f;
        public float intensity = 1f;
        public bool overrideColors;
        public Color overridePrimaryColor = Color.white;
        public Color overrideSecondaryColor = Color.gray;
    }
}
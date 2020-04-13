﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using EnhancedStreamChat.Chat;
using EnhancedStreamChat.Textures;
using System.Collections;
using static POCs.Sanjay.SharpSnippets.Drawing.ColorExtensions;
using Random = System.Random;
using System.Reflection;
using EnhancedStreamChat.Config;
using EnhancedStreamChat.Images;
using StreamCore;
using StreamCore.Chat;
using StreamCore.Utils;

namespace EnhancedStreamChat.UI
{
    public class CustomImage : Image
    {
        public string spriteIndex;
        public ImageType imageType;
        public Shadow shadow;
        public CachedSpriteData cachedTextureData = null;
        internal bool isShadow = false;
        public override Material material
        {
            get
            {
                return base.material;
            }
            set
            {
                // Only allow setting our custom image material from our own assembly, incase anything else tries to set it so it doensn't break the animation
                if (Assembly.GetCallingAssembly() == Assembly.GetExecutingAssembly())
                    base.material = value;
            }
        }
        protected override void Awake()
        {
            base.Awake();

            shadow = gameObject.AddComponent<Shadow>();
            shadow.effectDistance = new Vector2(10f, -10f);
        }
    }

    public class CustomText : Text
    {
        public ChatMessage messageInfo;
        public List<CustomImage> emoteRenderers = new List<CustomImage>();
        public bool hasRendered = false;
    };

    [RequireComponent(typeof(LayoutElement))]
    public class ContentSizeManager : MonoBehaviour
    {
        private LayoutElement _thisElement;
        private float _width;

        public void Init()
        {
            _thisElement = GetComponent<LayoutElement>();
        }

        private void FixedUpdate()
        {
            if (ChatConfig.instance.ChatWidth != _width)
            {
                _thisElement.preferredWidth = ChatConfig.instance.ChatWidth;
                _width = ChatConfig.instance.ChatWidth;
            }
        }
    };

    class Drawing
    {
        public static int pixelsPerUnit = 100;
        public static Material noGlowMaterial = null;
        public static Material noGlowMaterialUI = null;
        public static Material clearMaterial = null;
        public static readonly string spacingChar = "\u202f";
        internal static readonly float badgeEmojiHeight = 80f;
        internal static readonly float emoteHeight = 120f;
        public static float imageSpacingWidth;
        private static Regex htmlRegex = new Regex("<color=#.*><b>(.*)</b></color>", RegexOptions.Compiled);

        public static bool MaterialsCached
        {
            get
            {
                if (!noGlowMaterial)
                {
                    Material material = Resources.FindObjectsOfTypeAll<Material>().Where(m => m.name == "UINoGlow").FirstOrDefault();
                    if (material)
                    {
                        noGlowMaterial = Material.Instantiate(material);
                        noGlowMaterialUI = Material.Instantiate(material);
                        noGlowMaterial.name = "UINoGlow (EnhancedStreamChat)";
                        noGlowMaterialUI.name = "UINoGlow (EnhancedStreamChat)";
                        ChatHandler.Instance.background.material = Material.Instantiate(material);
                        ChatHandler.Instance.lockButtonImage.material = Material.Instantiate(material);
                        clearMaterial = Material.Instantiate(material);
                        clearMaterial.color = Color.clear;
                        ChatHandler.Instance.chatMoverPrimitive.GetComponent<Renderer>().material = clearMaterial;
                        ChatHandler.Instance.lockButtonPrimitive.GetComponent<Renderer>().material = clearMaterial;
                    }
                }
                return noGlowMaterial && noGlowMaterialUI;
            }
        }

        private static AssetBundle _assets = null;
        private static AssetBundle Assets
        {
            get
            {
                if (!_assets)
                    _assets = AssetBundle.LoadFromMemory(Utilities.GetResource(Assembly.GetExecutingAssembly(), "EnhancedStreamChat.Resources.Assets"));
                return _assets;
            }
        }

        private static Material _cropMaterial = null;
        public static Material CropMaterial
        {
            get
            {
                if (!_cropMaterial)
                    _cropMaterial = new Material(Assets.LoadAsset<Shader>("Crop"));
                return _cropMaterial;
            }
        }

        private static Material _cropMaterialColorMultiply = null;
        public static Material CropMaterialColorMultiply
        {
            get
            {
                if (!_cropMaterialColorMultiply)
                    _cropMaterialColorMultiply = new Material(Assets.LoadAsset<Shader>("CropColourMultiply"));
                return _cropMaterialColorMultiply;
            }
        }

        public static IEnumerator Initialize(Transform parent)
        {
            CustomText tmpText = InitText(spacingChar, Color.clear, ChatConfig.instance.ChatScale, new Vector2(ChatConfig.instance.ChatWidth, 1), new Vector3(0, -100, 0), new Quaternion(0, 0, 0, 0), parent, TextAnchor.UpperLeft, false);
            yield return null;
            imageSpacingWidth = tmpText.preferredWidth;
            //Plugin.Log($"Preferred width was {tmpText.preferredWidth.ToString()} with {tmpText.text.Length.ToString()} spaces");
            GameObject.Destroy(tmpText.gameObject);
        }

        public static Font LoadSystemFont(string font)
        {
            bool useFallback = false;
            if (font.Length > 0)
            {
                List<string> matchingFonts = Font.GetOSInstalledFontNames().ToList().Where(f => f.ToLower() == font.ToLower()).ToList();
                if (matchingFonts.Count == 0)
                    useFallback = true;
                else
                    font = matchingFonts.First();
            }
            else
                useFallback = true;

            if (useFallback)
            {
                font = "Segoe UI";
                ChatConfig.instance.FontName = font;
                ChatConfig.instance.Save();
                Plugin.Log($"Invalid font name specified! Falling back to Segoe UI");
            }
            return Font.CreateDynamicFontFromOSFont(font, 10);
        }

        public static CustomText InitText(string text, Color textColor, float fontSize, Vector2 sizeDelta, Vector3 position, Quaternion rotation, Transform parent, TextAnchor textAlign, bool wrapText, Material mat = null)
        {
            GameObject newGameObj = new GameObject("CustomText");
            CustomText tmpText = newGameObj.AddComponent<CustomText>();
            if (wrapText)
            {
                var mcs = newGameObj.AddComponent<ContentSizeManager>();
                mcs.transform.SetParent(tmpText.rectTransform, false);
                mcs.Init();
                var fitter = newGameObj.AddComponent<ContentSizeFitter>();
                fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
            newGameObj.AddComponent<Shadow>();

            CanvasScaler scaler = newGameObj.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = pixelsPerUnit;
            tmpText.rectTransform.SetParent(parent, false);
            tmpText.rectTransform.localPosition = position;
            tmpText.rectTransform.localRotation = rotation;
            tmpText.rectTransform.pivot = new Vector2(0, 0);
            tmpText.rectTransform.sizeDelta = sizeDelta;
            tmpText.supportRichText = true;
            tmpText.font = LoadSystemFont(ChatConfig.instance.FontName);
            tmpText.text = text;
            tmpText.fontSize = 10;
            tmpText.verticalOverflow = VerticalWrapMode.Overflow;
            tmpText.alignment = textAlign;
            tmpText.horizontalOverflow = HorizontalWrapMode.Wrap;
            tmpText.color = textColor;
            tmpText.material.renderQueue = 3001;
            //tmpText.resizeTextForBestFit = true;

            if (mat)
                tmpText.material = mat;

            return tmpText;
        }

        public static void OverlayImage(CustomText currentMessage, ImageInfo imageInfo)
        {
            CachedSpriteData cachedTextureData = imageInfo.cachedSprite;

            // If cachedTextureData is null, the emote will be overlayed at a later time once it's finished being cached
            if (cachedTextureData == null || (cachedTextureData.sprite == null && cachedTextureData.animInfo == null))
                return;

            bool animatedEmote = cachedTextureData.animInfo != null;
            var messageWithoutHtml = htmlRegex.Replace(currentMessage.text, (m) => m.Groups[1].Value);
            foreach (int i in EmojiUtilities.IndexOfAll(messageWithoutHtml, Char.ConvertFromUtf32(imageInfo.swapChar)))
            {
                CustomImage image = null, shadow = null;
                try
                {
                    if (i > 0 && i < currentMessage.text.Length)
                    {
                        image = ChatHandler.Instance.imagePool.Alloc();
                        image.cachedTextureData = cachedTextureData;
                        image.spriteIndex = imageInfo.textureIndex;
                        image.imageType = imageInfo.imageType;
                        image.sprite = cachedTextureData.sprite;
                        image.preserveAspect = false;
                        if (image.sprite)
                            image.sprite.texture.wrapMode = TextureWrapMode.Clamp;

                        image.rectTransform.SetParent(currentMessage.rectTransform, false);
                        image.rectTransform.localScale = new Vector3(0.064f, 0.064f, 0.064f);
                        image.rectTransform.sizeDelta = new Vector2(cachedTextureData.width, cachedTextureData.height);

                        if(imageInfo.imageType == ImageType.Emoji || imageInfo.imageType == ImageType.Badge)
                            image.rectTransform.pivot = new Vector2(0, 0.21f);
                        else
                            image.rectTransform.pivot = new Vector2(0, 0.3f);

                        TextGenerator textGen = currentMessage.cachedTextGenerator;
                        Vector3 pos = new Vector3(textGen.verts[i * 4].position.x, textGen.verts[i * 4].position.y);
                        image.rectTransform.position = currentMessage.gameObject.transform.TransformPoint(pos / pixelsPerUnit + new Vector3(0,0,-0.1f));

                        if (animatedEmote)
                        {
                            cachedTextureData.animInfo?.animData?.IncRefs();
                            image.material = cachedTextureData.animInfo.imageMaterial;
                            if (ChatConfig.instance.DrawShadows && cachedTextureData.animInfo.shadowMaterial != null)
                            {
                                // Add a shadow to our animated image (the regular unity shadows won't work with this material)
                                shadow = ChatHandler.Instance.imagePool.Alloc();
                                shadow.material = cachedTextureData.animInfo.shadowMaterial;
                                shadow.sprite = null;
                                shadow.spriteIndex = imageInfo.textureIndex;
                                shadow.imageType = imageInfo.imageType;
                                shadow.rectTransform.pivot = new Vector2(0, 0);
                                shadow.rectTransform.localScale = image.rectTransform.localScale;
                                shadow.rectTransform.SetParent(currentMessage.rectTransform, false);
                                shadow.rectTransform.position = image.rectTransform.position;
                                shadow.rectTransform.localPosition += new Vector3(0.6f, -0.6f, 0.05f);
                                shadow.enabled = true;
                                currentMessage.emoteRenderers.Add(shadow);
                            }
                        }
                        else
                        {
                            image.material = noGlowMaterialUI;
                            if (ChatConfig.instance.DrawShadows)
                                image.shadow.enabled = true;
                        }
                        image.enabled = true;
                        currentMessage.emoteRenderers.Add(image);
                    }
                }
                catch (Exception e)
                {
                    if (image)
                        ChatHandler.Instance.imagePool.Free(image);

                    Plugin.Log($"Exception {e.ToString()} occured when trying to overlay emote at index {i.ToString()}!");
                }
            }
        }

        public static System.Drawing.Color GetPastelShade(System.Drawing.Color source)
        {
            return (generateColor(source, true, new HSB { H = 0, S = 0.2d, B = 255 }, new HSB { H = 360, S = 0.5d, B = 255 }));
        }

        static Random randomizer = new Random();
        private static System.Drawing.Color generateColor(System.Drawing.Color source, bool isaShadeOfSource, HSB min, HSB max)
        {
            HSB hsbValues = ConvertToHSB(new RGB { R = source.R, G = source.G, B = source.B });
            double h_double = randomizer.NextDouble();
            double s_double = randomizer.NextDouble();
            double b_double = randomizer.NextDouble();
            if (max.B - min.B == 0) b_double = 0; //do not change Brightness
            if (isaShadeOfSource)
            {
                min.H = hsbValues.H;
                max.H = hsbValues.H;
                h_double = 0;
            }
            hsbValues = new HSB
            {
                H = Convert.ToDouble(randomizer.Next(Convert.ToInt32(min.H), Convert.ToInt32(max.H))) + h_double,
                S = Convert.ToDouble((randomizer.Next(Convert.ToInt32(min.S * 100), Convert.ToInt32(max.S * 100))) / 100d),
                B = Convert.ToDouble(randomizer.Next(Convert.ToInt32(min.B), Convert.ToInt32(max.B))) + b_double
            };
            //Debug.WriteLine("H:{0} | S:{1} | B:{2} [Min_S:{3} | Max_S{4}]", hsbValues.H, _hsbValues.S, _hsbValues.B, min.S, max.S);
            RGB rgbvalues = ConvertToRGB(hsbValues);
            return System.Drawing.Color.FromArgb(source.A, (int)rgbvalues.R, (int)rgbvalues.G, (int)rgbvalues.B);
        }
    };

}

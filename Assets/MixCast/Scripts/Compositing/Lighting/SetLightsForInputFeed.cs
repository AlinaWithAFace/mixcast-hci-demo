/**********************************************************************************
* Blueprint Reality Inc. CONFIDENTIAL
* 2018 Blueprint Reality Inc.
* All Rights Reserved.
*
* NOTICE:  All information contained herein is, and remains, the property of
* Blueprint Reality Inc. and its suppliers, if any.  The intellectual and
* technical concepts contained herein are proprietary to Blueprint Reality Inc.
* and its suppliers and may be covered by Patents, pending patents, and are
* protected by trade secret or copyright law.
*
* Dissemination of this information or reproduction of this material is strictly
* forbidden unless prior written permission is obtained from Blueprint Reality Inc.
***********************************************************************************/

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlueprintReality.MixCast
{
	public class SetLightsForInputFeed : MonoBehaviour
    {
        public class FrameLightingData
        {
            public int directionalLightCount;
            public Vector4[] directionalLightDirections;
            public Vector4[] directionalLightColors;

            public int pointLightCount;
            public Vector4[] pointLightPositions;
            public Vector4[] pointLightColors;

            public int spotLightCount;
            public Vector4[] spotLightPositions;
            public Vector4[] spotLightDirections;
            public Vector4[] spotLightColors;
        }

        protected const int DIR_LIGHT_ARRAY_MAX = 10;
        protected const int POINT_LIGHT_ARRAY_MAX = 100;
        protected const int SPOT_LIGHT_ARRAY_MAX = 100;


        protected const string DIR_LIGHT_ARRAY_SIZE = "_DirLightCount";
        protected const string DIR_LIGHT_DIRECTIONS = "_DirLightDirections";
        protected const string DIR_LIGHT_COLORS = "_DirLightColors";

        protected const string POINT_LIGHT_ARRAY_SIZE = "_PointLightCount";
        protected const string POINT_LIGHT_POSITIONS_AND_RANGE = "_PointLightPositions";
        protected const string POINT_LIGHT_COLORS = "_PointLightColors";

        protected const string SPOT_LIGHT_ARRAY_SIZE = "_SpotLightCount";
        protected const string SPOT_LIGHT_POSITION_AND_RANGE = "_SpotLightPositions";
        protected const string SPOT_LIGHT_DIRECTIONS_AND_ANGLE = "_SpotLightDirections";
        protected const string SPOT_LIGHT_COLORS = "_SpotLightColors";


        public InputFeed feed;

        public string playerLayerName = "Default";

        public bool includeBacklighting = true;


        private int layerNum;
        private FrameDelayQueue<FrameLightingData> frames;

        private void OnEnable()
        {
            if (feed == null)
                feed = GetComponentInParent<InputFeed>();

            layerNum = LayerMask.NameToLayer(playerLayerName);

            frames = new FrameDelayQueue<FrameLightingData>();

            feed.OnRenderStarted += HandleRenderStarted;
            feed.OnRenderEnded += HandleRenderEnded;
        }

        private void OnDisable()
        {
            frames = null;

            feed.OnRenderStarted -= HandleRenderStarted;
            feed.OnRenderEnded -= HandleRenderEnded;
        }

        private void HandleRenderStarted()
        {
            frames.delayDuration = feed.context.Data.unplugged == false && feed.context.Data.outputMode == MixCastData.OutputMode.Buffered ? feed.context.Data.bufferTime : 0;
            frames.Update();

            if (!feed.context.Data.lightingData.isEnabled
                || feed.context.Data.lightingData.effectAmount <= 0)
                return;

            feed.blitMaterial.EnableKeyword("LIGHTING_FLAT");

            FrameLightingData nextFrame = PrepareNextFrame();
            CalculateCurrentLightsData(nextFrame);

            FrameLightingData oldFrame = frames.OldestFrameData;

            feed.blitMaterial.SetFloat("_PlayerLightingFactor", feed.context.Data.lightingData.effectAmount);
            feed.blitMaterial.SetFloat("_PlayerLightingBase", feed.context.Data.lightingData.baseLighting);
            feed.blitMaterial.SetFloat("_PlayerLightingPower", feed.context.Data.lightingData.powerMultiplier);

            feed.blitMaterial.SetInt(DIR_LIGHT_ARRAY_SIZE, oldFrame.directionalLightCount);
            feed.blitMaterial.SetVectorArray(DIR_LIGHT_DIRECTIONS, oldFrame.directionalLightDirections);
            feed.blitMaterial.SetVectorArray(DIR_LIGHT_COLORS, oldFrame.directionalLightColors);

            feed.blitMaterial.SetInt(POINT_LIGHT_ARRAY_SIZE, oldFrame.pointLightCount);
            feed.blitMaterial.SetVectorArray(POINT_LIGHT_POSITIONS_AND_RANGE, oldFrame.pointLightPositions);
            feed.blitMaterial.SetVectorArray(POINT_LIGHT_COLORS, oldFrame.pointLightColors);

            feed.blitMaterial.SetInt(SPOT_LIGHT_ARRAY_SIZE, oldFrame.spotLightCount);
            feed.blitMaterial.SetVectorArray(SPOT_LIGHT_POSITION_AND_RANGE, oldFrame.spotLightPositions);
            feed.blitMaterial.SetVectorArray(SPOT_LIGHT_DIRECTIONS_AND_ANGLE, oldFrame.spotLightDirections);
            feed.blitMaterial.SetVectorArray(SPOT_LIGHT_COLORS, oldFrame.spotLightColors);
        }
        private void HandleRenderEnded()
        {
            feed.blitMaterial.DisableKeyword("LIGHTING_FLAT");
        }

        void CalculateCurrentLightsData(FrameLightingData lightingData)
        {
            MixCastCamera cam = MixCastCamera.FindCamera(feed.context);
            if (cam == null || cam.gameCamera == null)
                return;

            float playerDist = feed.CalculatePlayerDistance(cam.gameCamera);

            lightingData.directionalLightCount = 0;
            if (MixCast.ProjectSettings.specifyLightsManually)
            {
                foreach( Light light in MixCastLight.ActiveDirectionalLights )
                {
                    if( (light.cullingMask & (1 << layerNum)) > 0 && LightIsAffectingPlayer(light, cam.gameCamera, playerDist))
                    {
                        lightingData.directionalLightDirections[lightingData.directionalLightCount] = light.transform.forward;
                        lightingData.directionalLightColors[lightingData.directionalLightCount] = light.color * light.intensity * MixCast.ProjectSettings.directionalLightPower * 0.5f;
                        lightingData.directionalLightCount++;

                        if (lightingData.directionalLightCount == DIR_LIGHT_ARRAY_MAX)
                            break;
                    }
                }
            }
            else
            {
                var directionalLights = Light.GetLights(LightType.Directional, layerNum);
                for (int i = 0; i < directionalLights.Length && lightingData.directionalLightCount < DIR_LIGHT_ARRAY_MAX; i++)
                {
                    if (LightIsAffectingPlayer(directionalLights[i], cam.gameCamera, playerDist))
                    {
                        lightingData.directionalLightDirections[lightingData.directionalLightCount] = directionalLights[i].transform.forward;
                        lightingData.directionalLightColors[lightingData.directionalLightCount] = directionalLights[i].color * directionalLights[i].intensity * MixCast.ProjectSettings.directionalLightPower * 0.5f;
                        lightingData.directionalLightCount++;
                    }
                }
            }


            lightingData.pointLightCount = 0;
            if (MixCast.ProjectSettings.specifyLightsManually)
            {
                foreach (Light light in MixCastLight.ActivePointLights)
                {
                    if ((light.cullingMask & (1 << layerNum)) > 0 && LightIsAffectingPlayer(light, cam.gameCamera, playerDist))
                    {
                        lightingData.pointLightPositions[lightingData.pointLightCount] = light.transform.position;
                        lightingData.pointLightPositions[lightingData.pointLightCount].w = light.range;
                        lightingData.pointLightColors[lightingData.pointLightCount] = light.color * light.intensity * MixCast.ProjectSettings.pointLightPower * 0.5f;
                        lightingData.pointLightCount++;

                        if (lightingData.pointLightCount == POINT_LIGHT_ARRAY_MAX)
                            break;
                    }
                }
            }
            else
            {
                var pointLights = Light.GetLights(LightType.Point, layerNum);
                for (int i = 0; i < pointLights.Length && lightingData.pointLightCount < POINT_LIGHT_ARRAY_MAX; i++)
                {
                    if (LightIsAffectingPlayer(pointLights[i], cam.gameCamera, playerDist))
                    {
                        lightingData.pointLightPositions[lightingData.pointLightCount] = pointLights[i].transform.position;
                        lightingData.pointLightPositions[lightingData.pointLightCount].w = pointLights[i].range;
                        lightingData.pointLightColors[lightingData.pointLightCount] = pointLights[i].color * pointLights[i].intensity * MixCast.ProjectSettings.pointLightPower * 0.5f;
                        lightingData.pointLightCount++;
                    }
                }
            }

            //foundLights = Light.GetLights(LightType.Spot, layerNum);
            //lightingData.spotLightCount = 0;
            //for (int i = 0; i < foundLights.Length && lightingData.spotLightCount < SPOT_LIGHT_ARRAY_MAX; i++)
            //{
            //    if (LightIsAffectingPlayer(foundLights[i], cam.gameCamera, playerDist))
            //    {
            //        lightingData.spotLightPositions[lightingData.spotLightCount] = foundLights[i].transform.position;
            //        lightingData.spotLightPositions[lightingData.spotLightCount].w = foundLights[i].range;
            //        lightingData.spotLightDirections[lightingData.spotLightCount] = foundLights[i].transform.forward;
            //        lightingData.spotLightDirections[lightingData.spotLightCount].w = foundLights[i].spotAngle * Mathf.Deg2Rad * 0.5f;
            //        lightingData.spotLightColors[lightingData.spotLightCount] = foundLights[i].color * foundLights[i].intensity * MixCast.ProjectSettings.spotLightPower * 0.5f;
            //        lightingData.spotLightCount++;
            //    }
            //}
        }

        FrameLightingData PrepareNextFrame()
        {
            FrameDelayQueue<FrameLightingData>.Frame<FrameLightingData> nextFrame = frames.GetNewFrame();
            if (nextFrame.data == null)
                nextFrame.data = new FrameLightingData();

            if (nextFrame.data.directionalLightDirections == null || nextFrame.data.directionalLightDirections.Length != DIR_LIGHT_ARRAY_MAX)
                nextFrame.data.directionalLightDirections = new Vector4[DIR_LIGHT_ARRAY_MAX];
            if (nextFrame.data.directionalLightColors == null || nextFrame.data.directionalLightColors.Length != DIR_LIGHT_ARRAY_MAX)
                nextFrame.data.directionalLightColors = new Vector4[DIR_LIGHT_ARRAY_MAX];

            if (nextFrame.data.pointLightPositions == null || nextFrame.data.pointLightPositions.Length != POINT_LIGHT_ARRAY_MAX)
                nextFrame.data.pointLightPositions = new Vector4[POINT_LIGHT_ARRAY_MAX];
            if (nextFrame.data.pointLightColors == null || nextFrame.data.pointLightColors.Length != POINT_LIGHT_ARRAY_MAX)
                nextFrame.data.pointLightColors = new Vector4[POINT_LIGHT_ARRAY_MAX];

            if (nextFrame.data.spotLightPositions == null || nextFrame.data.spotLightPositions.Length != SPOT_LIGHT_ARRAY_MAX)
                nextFrame.data.spotLightPositions = new Vector4[SPOT_LIGHT_ARRAY_MAX];
            if (nextFrame.data.spotLightDirections == null || nextFrame.data.spotLightDirections.Length != SPOT_LIGHT_ARRAY_MAX)
                nextFrame.data.spotLightDirections = new Vector4[SPOT_LIGHT_ARRAY_MAX];
            if (nextFrame.data.spotLightColors == null || nextFrame.data.spotLightColors.Length != SPOT_LIGHT_ARRAY_MAX)
                nextFrame.data.spotLightColors = new Vector4[SPOT_LIGHT_ARRAY_MAX];

            return nextFrame.data;
        }

        bool LightIsAffectingPlayer(Light l, Camera cam, float playerDist)
        {
            switch(l.type)
            {
                case LightType.Directional:
                    return true;

                case LightType.Point:
                    float dot = Vector3.Dot(cam.transform.forward, l.transform.position - cam.transform.position);

                    if (Mathf.Abs(dot - playerDist) > l.range)
                        return false;
                    if (playerDist < dot && !includeBacklighting)
                        return false;

                    return true;

                case LightType.Spot:
                    float coneSideLength = l.range / Mathf.Cos(l.spotAngle * Mathf.Deg2Rad);

                    float dot2 = Vector3.Dot(cam.transform.forward, l.transform.position - cam.transform.position);
                    if (Mathf.Abs(dot2 - playerDist) > coneSideLength)
                        return false;
                    if (playerDist < dot2 && !includeBacklighting)
                        return false;
                    return true;

                default:
                    return false;
            }
        }
    }
}
#endif

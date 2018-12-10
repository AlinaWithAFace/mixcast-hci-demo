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
namespace BlueprintReality.MixCast
{
    public class ShowPopupOnScreenshotEvent : ToastEventHandler
    {
        protected override string Category { get { return EventCenter.Category.Saving; } }

        protected override string ResultStartedTitle { get { return "Action_OutputScreenshot"; } }
        protected override string ResultStoppedTitle { get { return "Action_OutputScreenshot"; } }
        protected override string ResultSuccessTitle { get { return "Action_OutputScreenshot"; } }
        protected override string ResultErrorTitle { get { return "Field_Warning"; } }

        protected override string ResultStartedMsg { get { return ""; } }
        protected override string ResultStoppedMsg { get { return ""; } }
        protected override string ResultSuccessMsg { get { return "Info_Saved_Screenshot"; } }
        protected override string ResultErrorMsg { get { return "Warning_Screenshot_Error"; } }
    }
}
#endif
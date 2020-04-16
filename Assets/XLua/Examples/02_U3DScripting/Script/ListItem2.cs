using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LuyuScrollView
{
    public class ListItem2 : MonoBehaviour
    {

        public Text mNameText;
        int mItemDataIndex = -1;
        
        public void SetItemData(ItemData itemData, int itemIndex)
        {
            mItemDataIndex = itemIndex;
            mNameText.text = itemData.mName;
            //mDescText.text = itemData.mFileSize.ToString() + "KB";
            //mDescText2.text = itemData.mDesc;
            //mIcon.sprite = ResManager.Get.GetSpriteByName(itemData.mIcon);
            //SetStarCount(itemData.mStarCount);
        }
    }
}


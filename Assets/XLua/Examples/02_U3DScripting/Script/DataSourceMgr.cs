using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LuyuScrollView
{
    public class ItemData
    {
        public int mId;
        public string mName;
        public int mFileSize;
        public string mDesc;
        public string mIcon;
        public int mStarCount;
        public bool mChecked;
        public bool mIsExpand;
    }


    public class DataSourceMgr : MonoBehaviour
    {

        List<ItemData> mItemDataList = new List<ItemData>();
        static DataSourceMgr instance = null;
        public int mTotalDataCount = 100;
        public static DataSourceMgr Get
        {
            get
            {
                if (instance == null)
                {
                    instance = Object.FindObjectOfType<DataSourceMgr>();
                }
                return instance;
            }
        }


        private void Awake()
        {
            Init();
        }

        public void Init()
        {
            DoRefreshDataSource();
        }

        void DoRefreshDataSource()
        {
            mItemDataList.Clear();
            for (int i = 0; i < mTotalDataCount; ++i)
            {
                ItemData tData = new ItemData();
                tData.mId = i;
                tData.mName = "Item" + i;
                //tData.mDesc = "Item Desc For Item " + i;
              //  tData.mIcon = ResManager.Get.GetSpriteNameByIndex(Random.Range(0, 24));
                //tData.mStarCount = Random.Range(0, 6);
                //tData.mFileSize = Random.Range(20, 999);
                //tData.mChecked = false;
                //tData.mIsExpand = false;
                mItemDataList.Add(tData);
            }
        }


        public int TotalItemCount
        {
            get
            {
                return mItemDataList.Count;
            }
        }

        public ItemData GetItemDataByIndex(int index)
        {
            if (index < 0 || index >= mItemDataList.Count)
            {
                return null;
            }
            return mItemDataList[index];
        }

    }

}

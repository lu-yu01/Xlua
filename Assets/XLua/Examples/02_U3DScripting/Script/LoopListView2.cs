using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace LuyuScrollView
{
    public enum ListItemArrangeType
    {
        TopToBottom,
     //   BottomToTop,
    }

    public enum SnapStatus
    {
        NoTargetSet = 0,
        TargetHasSet = 1,
        SnapMoving = 2,
        SnapMoveFinish = 3
    }

    [System.Serializable]
    public class ItemPrefabConfData
    {
        public GameObject mItemPrefab = null;
        public float mPadding = 0;
        public int mInitCreateCount = 0;
        public float mStartPosOffset = 0;
    }

    public class ItemPool
    {
        GameObject mPrefabObj;
        string mPrefabName;
        int mInitCreateCount = 1;
        float mPadding = 0;
        float mStartPosOffset = 0;
        List<LoopListViewItem2> mTmpPooledItemList = new List<LoopListViewItem2>();
        List<LoopListViewItem2> mPooledItemList = new List<LoopListViewItem2>();
        private static int mCurItemIdCount = 0;
        RectTransform mItemParent = null;   //Content 
        public ItemPool()
        {

        }
        public void Init(GameObject prefabObj, float padding, float startPosOffset, int createCount, RectTransform parent)
        {
            mPrefabObj = prefabObj;
            mPrefabName = mPrefabObj.name;
            mInitCreateCount = createCount;
            mPadding = padding;
            mStartPosOffset = startPosOffset;
            mItemParent = parent;
            mPrefabObj.SetActive(false);
            for (int i = 0; i < mInitCreateCount; ++i)
            {
                LoopListViewItem2 tViewItem = CreateItem();
                RecycleItemReal(tViewItem);

            }
        }

        public LoopListViewItem2 GetItem()
        {
            mCurItemIdCount++;
            LoopListViewItem2 tItem = null;
            if (mTmpPooledItemList.Count > 0)
            {
                int count = mTmpPooledItemList.Count;
                tItem = mTmpPooledItemList[count - 1];
                mTmpPooledItemList.RemoveAt(count - 1);
                tItem.gameObject.SetActive(true);
            }
            else
            {
                int count = mPooledItemList.Count;
                if (count == 0)
                {
                    tItem = CreateItem();
                }
                else
                {
                    tItem = mPooledItemList[count - 1];
                    mPooledItemList.RemoveAt(count - 1);
                    tItem.gameObject.SetActive(true);
                }
            }
            tItem.Padding = mPadding;
            tItem.ItemId = mCurItemIdCount;
            return tItem;

        }


        public LoopListViewItem2 CreateItem()
        {
            GameObject go = GameObject.Instantiate<GameObject>(mPrefabObj, Vector3.zero, Quaternion.identity, mItemParent);
            go.SetActive(true);
            RectTransform rf = go.GetComponent<RectTransform>();
            rf.localScale = Vector3.zero;
            rf.anchoredPosition3D = Vector3.zero;
            rf.localEulerAngles = Vector3.zero;
            LoopListViewItem2 tViewItem = go.GetComponent<LoopListViewItem2>();
            tViewItem.ItemPrefabName = mPrefabName;
            tViewItem.StartPosOffset = mStartPosOffset;
            return tViewItem;
        }

        private void RecycleItemReal(LoopListViewItem2 item)
        {
            item.gameObject.SetActive(false);
            mPooledItemList.Add(item);
        }
        public void RecycleItem(LoopListViewItem2 item)
        {
            mTmpPooledItemList.Add(item);
        }

        public void ClearTmpRecycledItem()
        {
            int count = mTmpPooledItemList.Count;
            if (count == 0)
            {
                return;
            }
            for (int i = 0; i < count; ++i)
            {
                RecycleItemReal(mTmpPooledItemList[i]);
            }
            mTmpPooledItemList.Clear();


        }

    }

    public class LoopListView2 : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
    {

        class SnapDarta
        {
            public SnapStatus mSnapStatus = SnapStatus.NoTargetSet;
            public int mSnapTargetIndex = 0;
            public float mTargetSnapVal = 0;
            public float mCurSnapVal = 0;
            public bool mIsForceSnapTo = false;
            public void Clear()
            {
                mSnapStatus = SnapStatus.NoTargetSet;
                mIsForceSnapTo = false;
            }
        }


        Dictionary<string, ItemPool> mItemPoolDict = new Dictionary<string, ItemPool>();
        List<ItemPool> mItemPoolList = new List<ItemPool>();
        ScrollRect mScrollRect = null;
        RectTransform mScrollRectTransform = null;
        RectTransform mContainerTrans = null;
        RectTransform mViewPortRectTransform = null;
        float mItemDefaultWithPaddingSize = 20;
        bool mIsVertList = false;
        int mLeftSnapUpdateExtraCount = 1;
        int mItemTotalCount = 0;
        bool mIsDraging = false;
        SnapDarta mCurSnapData = new SnapDarta();
        [SerializeField]
        private ListItemArrangeType mArrangeType = ListItemArrangeType.TopToBottom;
        public ListItemArrangeType ArrangeType
        {
            get
            {
                return mArrangeType;
            }
            set
            {
                mArrangeType = value;
            }
        }

        List<LoopListViewItem2> mItemList = new List<LoopListViewItem2>();

        [SerializeField]
        bool mSupportScrollBar = true;
        [SerializeField]
        List<ItemPrefabConfData> mItemPrefabDataList = new List<ItemPrefabConfData>();
        System.Func<LoopListView2, int, LoopListViewItem2> mOnGetItemByIndex;

        public System.Action<LoopListView2, LoopListViewItem2> mOnSnapItemFinished = null;
        public System.Action<LoopListView2, LoopListViewItem2> mOnSnapNearestChanged = null;

        Vector3[] mItemWorldCorners = new Vector3[4];
        Vector3[] mViewPortRectLocalCorners = new Vector3[4];
        bool mListViewInited = false;
        ItemPosMgr mItemPosMgr = null;
        int mCurReadyMinItemIndex = 0;
        int mCurReadyMaxItemIndex = 0;
        bool mNeedCheckNextMinItem = true;
        bool mNeedCheckNextMaxItem = true;
        float mLastItemPadding = 0;
        int mListUpdateCheckFrameCount = 0;
        int mLastItemIndex = 0;
        bool mNeedAdjustVec = false;
        float mDistanceForRecycle0 = 300;
        float mDistanceForNew0 = 200;
        float mDistanceForRecycle1 = 300;
        float mDistanceForNew1 = 200;
        bool mItemSnapEnable = false;
        Vector3 mLastSnapCheckPos = Vector3.zero;
        Vector2 mAdjustedVec;
        Vector3 mLastFrameContainerPos = Vector3.zero;
        [SerializeField]
        Vector2 mViewPortSnapPivot = Vector2.zero;
        [SerializeField]
        Vector2 mItemSnapPivot = Vector2.zero;
        int mCurSnapNearestItemIndex = -1;
        float mSmoothDumpVel = 0;
        float mSmoothDumpRate = 0.3f;
        float mSnapFinishThreshold = 0.1f;
        float mSnapVecThreshold = 145;
        PointerEventData mPointerEventData = null;
        public bool IsVertList
        {
            get
            {
                return mIsVertList;
            }

        }

        public float ViewPortWidth
        {
            get { return mViewPortRectTransform.rect.width; }
        }
        public float ViewPortHeight
        {
            get { return mViewPortRectTransform.rect.height; }
        }

        public void initListView(int itemTotalCount, System.Func<LoopListView2, int, LoopListViewItem2> onGetItemIndex)
        {
            mScrollRect = gameObject.GetComponent<ScrollRect>();
            if (mScrollRect == null)
            {
                Debug.LogError("ListView Init Failed! ScrollRect component not found!");
            }
            mCurSnapData.Clear();
            mItemPosMgr = new ItemPosMgr(mItemDefaultWithPaddingSize);
            mScrollRectTransform = mScrollRect.GetComponent<RectTransform>();
            mContainerTrans = mScrollRect.content;
            mViewPortRectTransform = mScrollRect.viewport;
            if (mViewPortRectTransform == null)
            {
                mViewPortRectTransform = mScrollRectTransform;
            }
            mIsVertList = (mArrangeType == ListItemArrangeType.TopToBottom);
            mScrollRect.horizontal = !mIsVertList;
            mScrollRect.vertical = mIsVertList;
            AdjustPivot(mViewPortRectTransform);
            AdjustAnchor(mContainerTrans);
            AdjustContainerPivot(mContainerTrans);
            InitItemPool();
            mOnGetItemByIndex = onGetItemIndex;
            if (mListViewInited == true)
            {
                Debug.LogError("LoopListView2.InitListView method can be called only once.");
            }
            mListViewInited = true;
            ResetListView();
            SetListItemCount(itemTotalCount, true);
        }

        private void Start()
        {

        }

         private void AdjustPivot(RectTransform rtf)
        {
            Vector2 pivot = rtf.pivot;
            switch (mArrangeType)
            {
                case ListItemArrangeType.TopToBottom:
                    pivot.y = 1;
                    break;
            }
            rtf.pivot = pivot;
        }

         private void AdjustAnchor(RectTransform rtf)
        {
            Vector2 anchorMin = rtf.anchorMin;
            Vector2 anchorMax = rtf.anchorMax;
            switch (mArrangeType)
            {
                case ListItemArrangeType.TopToBottom:
                    anchorMin.y = 1;
                    anchorMax.y = 1;
                    break;
            }
            rtf.anchorMin = anchorMin;
            rtf.anchorMax = anchorMax;
        }

         private void AdjustContainerPivot(RectTransform rtf)
        {
            Vector2 pivot = rtf.pivot;
            switch (mArrangeType)
            {
                case ListItemArrangeType.TopToBottom:
                    pivot.y = 1;
                    break;
            }
            rtf.pivot = pivot;
        }

        private void InitItemPool()
        {
            foreach (ItemPrefabConfData data in mItemPrefabDataList)
            {
                if (data.mItemPrefab == null)
                {
                    Debug.LogError("A item prefab is null");
                    continue;
                }
                string prefabName = data.mItemPrefab.name;
                if (mItemPoolDict.ContainsKey(prefabName))
                {
                    Debug.LogError("A item prefab with name" + prefabName + "has existed");
                    continue;
                }
                RectTransform rtf = data.mItemPrefab.GetComponent<RectTransform>();
                if (rtf == null)
                {
                    Debug.LogError("RectTrasform component is not found in the prefab" + prefabName);
                    continue;
                }
                AdjustAnchor(rtf);
                AdjustPivot(rtf);
                LoopListViewItem2 tItem = data.mItemPrefab.GetComponent<LoopListViewItem2>();
                if (tItem == null)
                {
                    data.mItemPrefab.AddComponent<LoopListViewItem2>();
                }
                ItemPool pool = new ItemPool();
                pool.Init(data.mItemPrefab, data.mPadding, data.mStartPosOffset, data.mInitCreateCount, mContainerTrans); //prefab Pool init
                mItemPoolDict.Add(prefabName, pool);
                mItemPoolList.Add(pool);
               }
        }

        public virtual void OnBeginDrag(PointerEventData eventData)
        {
            mIsDraging = true;
           // CacheDragPointerEventData(eventData);
            mCurSnapData.Clear();
        }

        public virtual void OnEndDrag(PointerEventData eventData)
        {
            mIsDraging = false;
            //mPointerEventData = null;
            ForceSnapUpdateCheck();
        }

        public virtual void OnDrag(PointerEventData eventData)
        {
            //Debug.Log("OnDrag");
          //  CacheDragPointerEventData(eventData);
        }

        /// <summary>
        /// CacheDragPointerEventData
        /// </summary>
        /// <param name="eventData"></param>
        //void CacheDragPointerEventData(PointerEventData eventData)
        //{
        //    if (mPointerEventData == null)
        //    {
        //        mPointerEventData = new PointerEventData(EventSystem.current);
        //    }
        //    mPointerEventData.button = eventData.button;
        //    mPointerEventData.position = eventData.position;
        //    mPointerEventData.pointerPressRaycast = eventData.pointerPressRaycast;
        //    mPointerEventData.pointerCurrentRaycast = eventData.pointerCurrentRaycast;
        //}



        public void ResetListView(bool resetPos = true)
        {
            mViewPortRectTransform.GetLocalCorners(mViewPortRectLocalCorners);
            if (resetPos)
            {
                mContainerTrans.anchoredPosition3D = Vector3.zero;
            }
            ForceSnapUpdateCheck();
        }

        public void ForceSnapUpdateCheck()
        {
            if (mLeftSnapUpdateExtraCount <= 0)
            {
                mLeftSnapUpdateExtraCount = 1;
            }
        }

        public void SetListItemCount(int itemCount, bool resetPos = true)
        {
            if (itemCount == mItemTotalCount)
            {
                return;
            }
            mCurSnapData.Clear();
            mItemTotalCount = itemCount;
            if (mItemTotalCount < 0)
            {
                mSupportScrollBar = false;
            }
            if (mSupportScrollBar)
            {
                mItemPosMgr.SetItemMaxCount(mItemTotalCount);
            }
            else
            {
                mItemPosMgr.SetItemMaxCount(0);
            }
            if (mItemTotalCount == 0)
            {
                mCurReadyMaxItemIndex = 0;
                mCurReadyMinItemIndex = 0;
                mNeedCheckNextMaxItem = false;
                mNeedCheckNextMinItem = false;
                RecycleAllItem();
                ClearAllTmpRecycledItem();
                UpdateContentSize();
                return;
            }
            if (mCurReadyMaxItemIndex >= mItemTotalCount)
            {
                mCurReadyMaxItemIndex = mItemTotalCount - 1;
            }
            mLeftSnapUpdateExtraCount = 1;
            mNeedCheckNextMaxItem = true;
            mNeedCheckNextMinItem = true;
            if (resetPos)
            {
                MovePanelToItemIndex(0, 0);
                return;
            }
            if (mItemList.Count == 0)
            {
                MovePanelToItemIndex(0, 0);
                return;
            }
            int maxItemIndex = mItemTotalCount - 1;
            int lastItemIndex = mItemList[mItemList.Count - 1].ItemIndex;
            if (lastItemIndex <= maxItemIndex)
            {
                UpdateContentSize();
                UpdateAllShownItemsPos();
                return;
            }
            MovePanelToItemIndex(maxItemIndex, 0);
        }

        void RecycleAllItem()
        {
            foreach (LoopListViewItem2 item in mItemList)
            {
                RecycleItemTmp(item);
            }
            mItemList.Clear();
        }

        private void RecycleItemTmp(LoopListViewItem2 item)
        {
            if (item == null)
            {
                return;
            }
            if (string.IsNullOrEmpty(item.ItemPrefabName))
            {
                return;
            }
            ItemPool pool = null;
            if (mItemPoolDict.TryGetValue(item.ItemPrefabName, out pool) == false)
            {
                return;
            }
            pool.RecycleItem(item);
        }

        void ClearAllTmpRecycledItem()
        {
            int count = mItemPoolList.Count;
            for (int i = 0; i <count; ++i)
            {
                mItemPoolList[i].ClearTmpRecycledItem();
            }
        }

        void UpdateContentSize()
        {
            float size = GetContentPanelSize();
            if (mIsVertList)
            {
                if (mContainerTrans.rect.height != size)
                {
                    mContainerTrans.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
                }
            }
            else
            {
                if (mContainerTrans.rect.width != size)
                {
                    mContainerTrans.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
                }
            }
        }

        private float GetContentPanelSize()
        {
            if (mSupportScrollBar)
            {
                float tTotalSize = mItemPosMgr.mTotalSize > 0 ? (mItemPosMgr.mTotalSize - mLastItemPadding) : 0;
                if (tTotalSize < 0)
                {
                    tTotalSize = 0;
                }
                return tTotalSize;
            }
            int count = mItemList.Count;
            if (count == 0)
            {
                return 0;
            }
            if (count == 1)
            {
                return mItemList[0].ItemSize;
            }
            if (count == 2)
            {
                return mItemList[0].ItemSizeWithPadding + mItemList[1].ItemSize;
            }
            float s = 0;
            for (int i = 0; i < count-1; ++i)
            {
                s += mItemList[i].ItemSizeWithPadding;
            }
            s += mItemList[count - 1].ItemSize;
            return s;
        }

        public void MovePanelToItemIndex(int itemIndex, float offset)
        {
            mScrollRect.StopMovement();
            mCurSnapData.Clear();
            if (itemIndex < 0 || mItemTotalCount == 0)
            {
                return;
            }
            if (mItemTotalCount > 0 && itemIndex >= mItemTotalCount)
            {
                itemIndex = mItemTotalCount - 1;
            }
            if (offset < 0)
            {
                offset = 0;
            }
            Vector3 pos = Vector3.zero;
            float viewPortSize = ViewPortSize;
            if (offset > viewPortSize)
            {
                offset = viewPortSize;
            }
            if (mArrangeType == ListItemArrangeType.TopToBottom)
            {
                float containerPos = mContainerTrans.anchoredPosition3D.y;
                if (containerPos < 0)
                {
                    containerPos = 0;
                }
                pos.y = -containerPos - offset;
            }
            RecycleAllItem();
            LoopListViewItem2 newItem = GetNewItemByIndex(itemIndex);//int first GetNewItemByIndex 
            if (newItem == null)
            {
                ClearAllTmpRecycledItem();
                return;
            }
            if (mIsVertList)
            {
                pos.x = newItem.StartPosOffset;
            }
            else
            {
                pos.y = newItem.StartPosOffset;
            }
            newItem.CachedRectTransform.anchoredPosition3D = pos;
            if (mSupportScrollBar)
            {
                if (mIsVertList)
                {
                    SetItemSize(itemIndex, newItem.CachedRectTransform.rect.height, newItem.Padding);
                }
                else
                {
                    SetItemSize(itemIndex, newItem.CachedRectTransform.rect.width, newItem.Padding);
                }
            }
            mItemList.Add(newItem);
            UpdateContentSize();
            UpdateListView(viewPortSize + 100, viewPortSize + 100, viewPortSize, viewPortSize);
            AdjustPanelPos();
            ClearAllTmpRecycledItem();


        }

        public float ViewPortSize
        {
            get
            {
                if (mIsVertList)
                {
                    return mViewPortRectTransform.rect.height;
                }
                else
                {
                    return mViewPortRectTransform.rect.width;
                }
            }
        }

        LoopListViewItem2 GetNewItemByIndex(int index)
        {
            if (mSupportScrollBar && index < 0)
            {
                return null;
            }
            if (mItemTotalCount > 0 && index >= mItemTotalCount)
            {
                return null;
            }
            Debug.LogError("index:" + index.ToString());
            LoopListViewItem2 newItem = mOnGetItemByIndex(this, index);
            if (newItem == null)
            {
                return null;
            }
            newItem.ItemIndex = index;
            newItem.ItemCreatedCheckFrameCount = mListUpdateCheckFrameCount;
            return newItem;

        }

        void SetItemSize(int itemIndex, float itemSize, float padding)
        {
            mItemPosMgr.SetItemSize(itemIndex, itemSize + padding);
            if (itemIndex >= mLastItemIndex)
            {
                mLastItemIndex = itemIndex;
                mLastItemPadding = padding;
            }
        }

         private void AdjustPanelPos()
        {
            int count = mItemList.Count;
            if (count == 0)
            {
                return;
            }
            UpdateAllShownItemsPos();
            float viewPortSize = ViewPortSize;
            float contentSize = GetContentPanelSize();
            if (mArrangeType == ListItemArrangeType.TopToBottom)
            {
                if (contentSize <= viewPortSize)
                {
                    Vector3 pos = mContainerTrans.anchoredPosition3D;
                    pos.y = 0;
                    mContainerTrans.anchoredPosition3D = pos;
                    mItemList[0].CachedRectTransform.anchoredPosition3D = new Vector3(mItemList[0].StartPosOffset, 0, 0);
                    UpdateAllShownItemsPos();
                    return;
                }
                LoopListViewItem2 tViewItem0 = mItemList[0];
                tViewItem0.CachedRectTransform.GetWorldCorners(mItemWorldCorners);
                Vector3 topPos0 = mViewPortRectTransform.InverseTransformPoint(mItemWorldCorners[1]);
                if (topPos0.y < mViewPortRectLocalCorners[1].y)
                {
                    Vector3 pos = mContainerTrans.anchoredPosition3D;
                    pos.y = 0;
                    mContainerTrans.anchoredPosition3D = pos;
                    mItemList[0].CachedRectTransform.anchoredPosition3D = new Vector3(mItemList[0].StartPosOffset, 0, 0);
                    UpdateAllShownItemsPos();
                    return;
                }
                LoopListViewItem2 tViewItem1 = mItemList[mItemList.Count - 1];
                tViewItem1.CachedRectTransform.GetWorldCorners(mItemWorldCorners);
                Vector3 downPos1 = mViewPortRectTransform.InverseTransformPoint(mItemWorldCorners[0]);
                float d = downPos1.y - mViewPortRectLocalCorners[0].y;
                if (d > 0)
                {
                    Vector3 pos = mItemList[0].CachedRectTransform.anchoredPosition3D;
                    pos.y = pos.y - d;
                    mItemList[0].CachedRectTransform.anchoredPosition3D = pos;
                    UpdateAllShownItemsPos();
                    return;
                }
            }
        }

        void UpdateAllShownItemsPos()
        {
            int count = mItemList.Count;
            if (count == 0)
            {
                return;
            }

            mAdjustedVec = (mContainerTrans.anchoredPosition3D - mLastFrameContainerPos) / Time.deltaTime;

            if (mArrangeType == ListItemArrangeType.TopToBottom)
            {
                float pos = 0;
                if (mSupportScrollBar)
                {
                    pos = -GetItemPos(mItemList[0].ItemIndex);
                }

                float pos1 = mItemList[0].CachedRectTransform.anchoredPosition3D.y;
                float d = pos - pos1;
                float curY = pos;
                for (int i = 0; i < count; ++i)
                {
                    LoopListViewItem2 item = mItemList[i];
                    item.CachedRectTransform.anchoredPosition3D = new Vector3(item.StartPosOffset, curY, 0);
                    curY = curY - item.CachedRectTransform.rect.height - item.Padding;
                }
                if (d != 0)
                {
                    Vector2 p = mContainerTrans.anchoredPosition3D;
                    p.y = p.y - d;
                    mContainerTrans.anchoredPosition3D = p;
                }
            }
            //if (mIsDraging)
            //{
            //    Debug.Log("mIsDraging:" + mIsDraging);
            //    mScrollRect.OnBeginDrag(mPointerEventData);
            //    mScrollRect.Rebuild(CanvasUpdate.PostLayout);
            //    mScrollRect.velocity = mAdjustedVec;
            //    mNeedAdjustVec = true;
            //}
        }

        float GetItemPos(int itemIndex)
        {
            return mItemPosMgr.GetItemPos(itemIndex);
        }

        public void UpdateListView(float distanceForRecycle0, float distanceForRecycle1, float distanceForNew0, float distanceForNew1)
        {
            //Debug.Log("UpdateListView");
            mListUpdateCheckFrameCount++;
            if (mIsVertList)
            {

                bool needContinueCheck = true;
                int checkCount = 0;
                int maxCount = 9999;
                while (needContinueCheck)
                {

                    checkCount++;
                    if (checkCount >= maxCount)
                    {
                        Debug.LogError("UpdateListView Vertical while loop " + checkCount + " times! something is wrong!");
                        break;
                    }
                    needContinueCheck = UpdateForVertList(distanceForRecycle0, distanceForRecycle1, distanceForNew0, distanceForNew1);
                    //Debug.LogFormat("checkCount:{0}  maxCount:{1} needContinueCheck:{2}", checkCount, maxCount, needContinueCheck);
                }
            }

        }

        bool UpdateForVertList(float distanceForRecycle0, float distanceForRecycle1, float distanceForNew0, float distanceForNew1)
        {
            if (mItemTotalCount == 0)
            {
                if (mItemList.Count > 0)
                {
                    RecycleAllItem();
                }
                return false;
            }
            if (mArrangeType == ListItemArrangeType.TopToBottom)//上到下
            {
                int itemListCount = mItemList.Count;
                //  Debug.Log("itemListCount:" + itemListCount);
                if (itemListCount == 0)
                {
                    float curY = mContainerTrans.anchoredPosition3D.y;
                    Debug.Log("curY:" + curY);
                    if (curY < 0)
                    {
                        curY = 0;
                    }
                    int index = 0;
                    float pos = -curY;
                    if (mSupportScrollBar)
                    {
                        GetPlusItemIndexAndPosAtGivenPos(curY, ref index, ref pos);
                        pos = -pos;
                    }
                    //Debug.LogError("GetNewItemByIndex:" + index);
                    LoopListViewItem2 newItem = GetNewItemByIndex(index);// GetNewItemCallback
                    if (newItem == null)
                    {
                        return false;
                    }
                    if (mSupportScrollBar)
                    {
                        SetItemSize(index, newItem.CachedRectTransform.rect.height, newItem.Padding);
                    }
                    mItemList.Add(newItem);
                    newItem.CachedRectTransform.anchoredPosition3D = new Vector3(newItem.StartPosOffset, pos, 0);
                    UpdateContentSize();
                    return true;
                }
                LoopListViewItem2 tViewItem0 = mItemList[0];
                tViewItem0.CachedRectTransform.GetWorldCorners(mItemWorldCorners);
                Vector3 topPos0 = mViewPortRectTransform.InverseTransformPoint(mItemWorldCorners[1]);
                Vector3 downPos0 = mViewPortRectTransform.InverseTransformPoint(mItemWorldCorners[0]);

                if (!mIsDraging && tViewItem0.ItemCreatedCheckFrameCount != mListUpdateCheckFrameCount
                    && downPos0.y - mViewPortRectLocalCorners[1].y > distanceForRecycle0)
                {
                    mItemList.RemoveAt(0);
                    RecycleItemTmp(tViewItem0);
                    if (!mSupportScrollBar)
                    {
                        UpdateContentSize();
                        CheckIfNeedUpdataItemPos();
                    }
                    return true;
                }

                LoopListViewItem2 tViewItem1 = mItemList[mItemList.Count - 1];
                tViewItem1.CachedRectTransform.GetWorldCorners(mItemWorldCorners);
                Vector3 topPos1 = mViewPortRectTransform.InverseTransformPoint(mItemWorldCorners[1]);
                Vector3 downPos1 = mViewPortRectTransform.InverseTransformPoint(mItemWorldCorners[0]);
                if (!mIsDraging && tViewItem1.ItemCreatedCheckFrameCount != mListUpdateCheckFrameCount
                    && mViewPortRectLocalCorners[0].y - topPos1.y > distanceForRecycle1)
                {
                    mItemList.RemoveAt(mItemList.Count - 1);
                    RecycleItemTmp(tViewItem1);
                    if (!mSupportScrollBar)
                    {
                        UpdateContentSize();
                        CheckIfNeedUpdataItemPos();
                    }
                    return true;
                }

                if (mViewPortRectLocalCorners[0].y - downPos1.y < distanceForNew1)
                {
                    Debug.LogFormat("data:{0} distanceForNew1:{1}", mViewPortRectLocalCorners[0].y - downPos1.y, distanceForNew1);
                    if (tViewItem1.ItemIndex > mCurReadyMaxItemIndex)
                    {
                        mCurReadyMaxItemIndex = tViewItem1.ItemIndex;
                        mNeedCheckNextMaxItem = true;
                    }
                    int nIndex = tViewItem1.ItemIndex + 1;
                    if (nIndex <= mCurReadyMaxItemIndex || mNeedCheckNextMaxItem)
                    Debug.LogFormat("nindex:{0} mCurReadyMaxItemIndex:{1}  mNeedCheckNextMaxItem:{2}", nIndex, mCurReadyMaxItemIndex, mNeedCheckNextMaxItem);
                    if (nIndex <= mCurReadyMaxItemIndex || mNeedCheckNextMaxItem)
                    {
                        Debug.LogError("nindex:" + nIndex);
                        LoopListViewItem2 newItem = GetNewItemByIndex(nIndex);// init scroll view
                        if (newItem == null)
                        {
                            mCurReadyMaxItemIndex = tViewItem1.ItemIndex;
                            mNeedCheckNextMaxItem = false;
                            CheckIfNeedUpdataItemPos();
                        }
                        else
                        {
                            if (mSupportScrollBar)
                            {
                                SetItemSize(nIndex, newItem.CachedRectTransform.rect.height, newItem.Padding);
                            }
                            mItemList.Add(newItem);
                            float y = tViewItem1.CachedRectTransform.anchoredPosition3D.y - tViewItem1.CachedRectTransform.rect.height - tViewItem1.Padding;
                            newItem.CachedRectTransform.anchoredPosition3D = new Vector3(newItem.StartPosOffset, y, 0);
                            UpdateContentSize();
                            CheckIfNeedUpdataItemPos();

                            if (nIndex > mCurReadyMaxItemIndex)
                            {
                                mCurReadyMaxItemIndex = nIndex;
                            }
                            return true;
                        }

                    }

                }

                if (topPos0.y - mViewPortRectLocalCorners[1].y < distanceForNew0)
                {
                    if (tViewItem0.ItemIndex < mCurReadyMinItemIndex)
                    {
                        mCurReadyMinItemIndex = tViewItem0.ItemIndex;
                        mNeedCheckNextMinItem = true;
                    }
                    int nIndex = tViewItem0.ItemIndex - 1;
                    if (nIndex >= mCurReadyMinItemIndex || mNeedCheckNextMinItem)
                    {
                        LoopListViewItem2 newItem = GetNewItemByIndex(nIndex);
                        if (newItem == null)
                        {
                            mCurReadyMinItemIndex = tViewItem0.ItemIndex;
                            mNeedCheckNextMinItem = false;
                        }
                        else
                        {
                            if (mSupportScrollBar)
                            {
                                SetItemSize(nIndex, newItem.CachedRectTransform.rect.height, newItem.Padding);
                            }
                            mItemList.Insert(0, newItem);
                            float y = tViewItem0.CachedRectTransform.anchoredPosition3D.y + newItem.CachedRectTransform.rect.height + newItem.Padding;
                            newItem.CachedRectTransform.anchoredPosition3D = new Vector3(newItem.StartPosOffset, y, 0);
                            UpdateContentSize();
                            CheckIfNeedUpdataItemPos();
                            if (nIndex < mCurReadyMinItemIndex)
                            {
                                mCurReadyMinItemIndex = nIndex;
                            }
                            return true;
                        }

                    }

                }

            }

            return false;
        }

        private void CheckIfNeedUpdataItemPos()
        {
            int count = mItemList.Count;
            if (count == 0)
            {
                return;
            }
            if (mArrangeType == ListItemArrangeType.TopToBottom)
            {
                LoopListViewItem2 firstItem = mItemList[0];
                LoopListViewItem2 lastItem = mItemList[mItemList.Count - 1];
                float viewMaxY = GetContentPanelSize();
                if (firstItem.TopY > 0 || (firstItem.ItemIndex == mCurReadyMinItemIndex && firstItem.TopY != 0))
                {
                    UpdateAllShownItemsPos();
                    return;
                }
                if ((-lastItem.BottomY) > viewMaxY || (lastItem.ItemIndex == mCurReadyMaxItemIndex && (-lastItem.BottomY) != viewMaxY))
                {
                    UpdateAllShownItemsPos();
                    return;
                }

            }
          }

        void GetPlusItemIndexAndPosAtGivenPos(float pos, ref int index, ref float itemPos)
        {
            mItemPosMgr.GetItemIndexAndPosAtGivenPos(pos, ref index, ref itemPos);
        }

        public LoopListViewItem2 NewListViewItem(string itemPrefabName)
        {
            ItemPool pool = null;
            if (mItemPoolDict.TryGetValue(itemPrefabName, out pool) == false)
            {
                return null;
            }
            LoopListViewItem2 item = pool.GetItem();
            RectTransform rf = item.GetComponent<RectTransform>();
            rf.SetParent(mContainerTrans);
            rf.localScale = Vector3.one;
            rf.anchoredPosition3D = Vector3.zero;
            rf.localEulerAngles = Vector3.zero;
            item.ParentListView = this;
            return item;
        }

        private void Update()
        {
            if (mListViewInited == false)
            {
                return;
            }
            //Debug.Log("111111111");
            //if (mNeedAdjustVec)
            //{
            //   mNeedAdjustVec = false;
            //    if (mIsVertList)
            //    {
                  
            //        if (mScrollRect.velocity.y * mAdjustedVec.y > 0)
            //        {
            //            mScrollRect.velocity = mAdjustedVec;
            //        }
            //    }
            //    else
            //    {
            //        if (mScrollRect.velocity.x * mAdjustedVec.x > 0)
            //        {
            //            mScrollRect.velocity = mAdjustedVec;
            //        }
            //    }
            //   }
            //UpdateSnapMove();
            UpdateListView(mDistanceForRecycle0, mDistanceForRecycle1, mDistanceForNew0, mDistanceForNew1);
            ClearAllTmpRecycledItem();
            mLastFrameContainerPos = mContainerTrans.anchoredPosition3D;
        }

        /// <summary>
        /// UpdateSnapMove
        /// </summary>
        /// <param name="immediate"></param>
        //void UpdateSnapMove(bool immediate = false)
        //{
        //    if (mItemSnapEnable == false)
        //    {
        //        return;
        //    }
        //    if (mIsVertList)
        //    {
        //        UpdateSnapVertical(immediate);
        //    }
        //}

        /// <summary>
        /// UpdateSnapVertical
        /// </summary>
        /// <param name="immediate"></param>
        //void UpdateSnapVertical(bool immediate = false)
        //{
        //    if (mItemSnapEnable == false)
        //    {
        //        return;
        //    }
        //    int count = mItemList.Count;
        //    if (count == 0)
        //    {
        //        return;
        //    }
        //    Vector3 pos = mContainerTrans.anchoredPosition3D;
        //    bool needCheck = (pos.y != mLastSnapCheckPos.y);
        //    mLastSnapCheckPos = pos;
        //    if (!needCheck)
        //    {
        //        if (mLeftSnapUpdateExtraCount > 0)
        //        {
        //            mLeftSnapUpdateExtraCount--;
        //            needCheck = true;
        //        }
        //    }
        //    if (needCheck)
        //    {
        //        LoopListViewItem2 tViewItem0 = mItemList[0];
        //        tViewItem0.CachedRectTransform.GetWorldCorners(mItemWorldCorners);
        //        int curIndex = -1;
        //        float start = 0;
        //        float end = 0;
        //        float itemSnapCenter = 0;
        //        float curMinDist = float.MaxValue;
        //        float curDist = 0;
        //        float curDistAbs = 0;
        //        float snapCenter = 0;
        //        if (mArrangeType == ListItemArrangeType.TopToBottom)
        //        {
        //            snapCenter = -(1 - mViewPortSnapPivot.y) * mViewPortRectTransform.rect.height;
        //            Vector3 topPos1 = mViewPortRectTransform.InverseTransformPoint(mItemWorldCorners[1]);
        //            start = topPos1.y;
        //            end = start - tViewItem0.ItemSizeWithPadding;
        //            itemSnapCenter = start - tViewItem0.ItemSize * (1 - mItemSnapPivot.y);
        //            for (int i = 0; i < count; ++i)
        //            {
        //                curDist = snapCenter - itemSnapCenter;
        //                curDistAbs = Mathf.Abs(curDist);
        //                if (curDistAbs < curMinDist)
        //                {
        //                    curMinDist = curDistAbs;
        //                    curIndex = i;
        //                }
        //                else
        //                {
        //                    break;
        //                }

        //                if ((i + 1) < count)
        //                {
        //                    start = end;
        //                    end = end - mItemList[i + 1].ItemSizeWithPadding;
        //                    itemSnapCenter = start - mItemList[i + 1].ItemSize * (1 - mItemSnapPivot.y);
        //                }
        //            }
        //        }
        //        else if (mArrangeType == ListItemArrangeType.BottomToTop)
        //        {
        //            snapCenter = mViewPortSnapPivot.y * mViewPortRectTransform.rect.height;
        //            Vector3 bottomPos1 = mViewPortRectTransform.InverseTransformPoint(mItemWorldCorners[0]);
        //            start = bottomPos1.y;
        //            end = start + tViewItem0.ItemSizeWithPadding;
        //            itemSnapCenter = start + tViewItem0.ItemSize * mItemSnapPivot.y;
        //            for (int i = 0; i < count; ++i)
        //            {
        //                curDist = snapCenter - itemSnapCenter;
        //                curDistAbs = Mathf.Abs(curDist);
        //                if (curDistAbs < curMinDist)
        //                {
        //                    curMinDist = curDistAbs;
        //                    curIndex = i;
        //                }
        //                else
        //                {
        //                    break;
        //                }

        //                if ((i + 1) < count)
        //                {
        //                    start = end;
        //                    end = end + mItemList[i + 1].ItemSizeWithPadding;
        //                    itemSnapCenter = start + mItemList[i + 1].ItemSize * mItemSnapPivot.y;
        //                }
        //            }
        //        }

        //        if (curIndex >= 0)
        //        {
        //            int oldNearestItemIndex = mCurSnapNearestItemIndex;
        //            mCurSnapNearestItemIndex = mItemList[curIndex].ItemIndex;
        //            if (mItemList[curIndex].ItemIndex != oldNearestItemIndex)
        //            {
        //                if (mOnSnapNearestChanged != null)
        //                {
        //                    mOnSnapNearestChanged(this, mItemList[curIndex]);
        //                }
        //            }
        //        }
        //        else
        //        {
        //            mCurSnapNearestItemIndex = -1;
        //        }
        //    }
        //    if (CanSnap() == false)
        //    {
        //        ClearSnapData();
        //        return;
        //    }   
        //    float v = Mathf.Abs(mScrollRect.velocity.y);
        //    UpdateCurSnapData();
        //    if (mCurSnapData.mSnapStatus != SnapStatus.SnapMoving)
        //    {
        //        return;
        //    }
        //    if (v > 0)
        //    {
        //        mScrollRect.StopMovement();
        //    }
        //    float old = mCurSnapData.mCurSnapVal;
        //    mCurSnapData.mCurSnapVal = Mathf.SmoothDamp(mCurSnapData.mCurSnapVal, mCurSnapData.mTargetSnapVal, ref mSmoothDumpVel, mSmoothDumpRate);
        //    float dt = mCurSnapData.mCurSnapVal - old;

        //    if (immediate || Mathf.Abs(mCurSnapData.mTargetSnapVal - mCurSnapData.mCurSnapVal) < mSnapFinishThreshold)
        //    {
        //        pos.y = pos.y + mCurSnapData.mTargetSnapVal - old;
        //        mCurSnapData.mSnapStatus = SnapStatus.SnapMoveFinish;
        //        if (mOnSnapItemFinished != null)
        //        {
        //            LoopListViewItem2 targetItem = GetShownItemByItemIndex(mCurSnapNearestItemIndex);
        //            if (targetItem != null)
        //            {
        //                mOnSnapItemFinished(this, targetItem);
        //            }
        //        }
        //    }
        //    else
        //    {
        //        pos.y = pos.y + dt;
        //    }

        //    if (mArrangeType == ListItemArrangeType.TopToBottom)
        //    {
        //        float maxY = mViewPortRectLocalCorners[0].y + mContainerTrans.rect.height;
        //        pos.y = Mathf.Clamp(pos.y, 0, maxY);
        //        mContainerTrans.anchoredPosition3D = pos;
        //    }
        //    else if (mArrangeType == ListItemArrangeType.BottomToTop)
        //    {
        //        float minY = mViewPortRectLocalCorners[1].y - mContainerTrans.rect.height;
        //        pos.y = Mathf.Clamp(pos.y, minY, 0);
        //        mContainerTrans.anchoredPosition3D = pos;
        //    }

        //}

        /// <summary>
        /// CanSnap
        /// </summary>
        /// <returns></returns>
        //bool CanSnap()
        //{
        //    if (mIsDraging)
        //    {
        //        return false;
        //    }
        //    //if (mScrollBarClickEventListener != null)
        //    //{
        //    //    if (mScrollBarClickEventListener.IsPressd)
        //    //    {
        //    //        return false;
        //    //    }
        //    //}

        //    if (mIsVertList)
        //    {
        //        if (mContainerTrans.rect.height <= ViewPortHeight)
        //        {
        //            return false;
        //        }
        //    }
        //    else
        //    {
        //        if (mContainerTrans.rect.width <= ViewPortWidth)
        //        {
        //            return false;
        //        }
        //    }

        //    float v = 0;
        //    if (mIsVertList)
        //    {
        //        v = Mathf.Abs(mScrollRect.velocity.y);
        //    }
        //    else
        //    {
        //        v = Mathf.Abs(mScrollRect.velocity.x);
        //    }
        //    if (v > mSnapVecThreshold)
        //    {
        //        return false;
        //    }
        //    if (v < 2)
        //    {
        //        return true;
        //    }
        //    float diff = 3;
        //    Vector3 pos = mContainerTrans.anchoredPosition3D;
        //     if (mArrangeType == ListItemArrangeType.TopToBottom)
        //    {
        //        float maxY = mViewPortRectLocalCorners[0].y + mContainerTrans.rect.height;
        //        if (pos.y > (maxY + diff) || pos.y < -diff)
        //        {
        //            return false;
        //        }
        //    }
        //    else if (mArrangeType == ListItemArrangeType.BottomToTop)
        //    {
        //        float minY = mViewPortRectLocalCorners[1].y - mContainerTrans.rect.height;
        //        if (pos.y < (minY - diff) || pos.y > diff)
        //        {
        //            return false;
        //        }
        //    }
        //    return true;
        //}

        public void ClearSnapData()
        {
            mCurSnapData.Clear();
        }

        /// <summary>
        /// UpdateCurSnapData
        /// </summary>
        //void UpdateCurSnapData()
        //{
        //    int count = mItemList.Count;
        //    if (count == 0)
        //    {
        //        mCurSnapData.Clear();
        //        return;
        //    }

        //    if (mCurSnapData.mSnapStatus == SnapStatus.SnapMoveFinish)
        //    {
        //        if (mCurSnapData.mSnapTargetIndex == mCurSnapNearestItemIndex)
        //        {
        //            return;
        //        }
        //        mCurSnapData.mSnapStatus = SnapStatus.NoTargetSet;
        //    }
        //    if (mCurSnapData.mSnapStatus == SnapStatus.SnapMoving)
        //    {
        //        if ((mCurSnapData.mSnapTargetIndex == mCurSnapNearestItemIndex) || mCurSnapData.mIsForceSnapTo)
        //        {
        //            return;
        //        }
        //        mCurSnapData.mSnapStatus = SnapStatus.NoTargetSet;
        //    }
        //    if (mCurSnapData.mSnapStatus == SnapStatus.NoTargetSet)
        //    {
        //        LoopListViewItem2 nearestItem = GetShownItemByItemIndex(mCurSnapNearestItemIndex);
        //        if (nearestItem == null)
        //        {
        //            return;
        //        }
        //        mCurSnapData.mSnapTargetIndex = mCurSnapNearestItemIndex;
        //        mCurSnapData.mSnapStatus = SnapStatus.TargetHasSet;
        //        mCurSnapData.mIsForceSnapTo = false;
        //    }
        //    if (mCurSnapData.mSnapStatus == SnapStatus.TargetHasSet)
        //    {
        //        LoopListViewItem2 targetItem = GetShownItemByItemIndex(mCurSnapData.mSnapTargetIndex);
        //        if (targetItem == null)
        //        {
        //            mCurSnapData.Clear();
        //            return;
        //        }
        //        UpdateAllShownItemSnapData();
        //        mCurSnapData.mTargetSnapVal = targetItem.DistanceWithViewPortSnapCenter;
        //        mCurSnapData.mCurSnapVal = 0;
        //        mCurSnapData.mSnapStatus = SnapStatus.SnapMoving;
        //    }

        //}

        public LoopListViewItem2 GetShownItemByItemIndex(int itemIndex)
        {
            int count = mItemList.Count;
            if (count == 0)
            {
                return null;
            }
            if (itemIndex < mItemList[0].ItemIndex || itemIndex > mItemList[count - 1].ItemIndex)
            {
                return null;
            }
            int i = itemIndex - mItemList[0].ItemIndex;
            return mItemList[i];
        }

        /// <summary>
        /// UpdateAllShownItemSnapData
        /// </summary>
        //void UpdateAllShownItemSnapData()
        //{
        //    if (mItemSnapEnable == false)
        //    {
        //        return;
        //    }
        //    int count = mItemList.Count;
        //    if (count == 0)
        //    {
        //        return;
        //    }
        //    Vector3 pos = mContainerTrans.anchoredPosition3D;
        //    LoopListViewItem2 tViewItem0 = mItemList[0];
        //    tViewItem0.CachedRectTransform.GetWorldCorners(mItemWorldCorners);
        //    float start = 0;
        //    float end = 0;
        //    float itemSnapCenter = 0;
        //    float snapCenter = 0;
        //    if (mArrangeType == ListItemArrangeType.TopToBottom)
        //    {
        //        snapCenter = -(1 - mViewPortSnapPivot.y) * mViewPortRectTransform.rect.height;
        //        Vector3 topPos1 = mViewPortRectTransform.InverseTransformPoint(mItemWorldCorners[1]);
        //        start = topPos1.y;
        //        end = start - tViewItem0.ItemSizeWithPadding;
        //        itemSnapCenter = start - tViewItem0.ItemSize * (1 - mItemSnapPivot.y);
        //        for (int i = 0; i < count; ++i)
        //        {
        //            mItemList[i].DistanceWithViewPortSnapCenter = snapCenter - itemSnapCenter;
        //            if ((i + 1) < count)
        //            {
        //                start = end;
        //                end = end - mItemList[i + 1].ItemSizeWithPadding;
        //                itemSnapCenter = start - mItemList[i + 1].ItemSize * (1 - mItemSnapPivot.y);
        //            }
        //        }
        //    }
        //    else if (mArrangeType == ListItemArrangeType.BottomToTop)
        //    {
        //        snapCenter = mViewPortSnapPivot.y * mViewPortRectTransform.rect.height;
        //        Vector3 bottomPos1 = mViewPortRectTransform.InverseTransformPoint(mItemWorldCorners[0]);
        //        start = bottomPos1.y;
        //        end = start + tViewItem0.ItemSizeWithPadding;
        //        itemSnapCenter = start + tViewItem0.ItemSize * mItemSnapPivot.y;
        //        for (int i = 0; i < count; ++i)
        //        {
        //            mItemList[i].DistanceWithViewPortSnapCenter = snapCenter - itemSnapCenter;
        //            if ((i + 1) < count)
        //            {
        //                start = end;
        //                end = end + mItemList[i + 1].ItemSizeWithPadding;
        //                itemSnapCenter = start + mItemList[i + 1].ItemSize * mItemSnapPivot.y;
        //            }
        //        }
        //    }
        //}
    }

}

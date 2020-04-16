using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LuyuScrollView
{
    public class BottomToTopDemo : MonoBehaviour
    {
        public LoopListView2 mLoopListView;
        void Start()
        {
            mLoopListView.initListView(DataSourceMgr.Get.TotalItemCount, OnGetItemByIndex);
        }

        LoopListViewItem2 OnGetItemByIndex(LoopListView2 listView, int index)
        {
            if (index < 0 || index >= DataSourceMgr.Get.TotalItemCount)
            {
                return null;
            }

            ItemData itemData = DataSourceMgr.Get.GetItemDataByIndex(index);
            if (itemData == null)
            {
                return null;
            }
            //get a new item. Every item can use a different prefab, the parameter of the NewListViewItem is the prefab’name. 
            //And all the prefabs should be listed in ItemPrefabList in LoopListView2 Inspector Setting
            LoopListViewItem2 item = listView.NewListViewItem("ItemPrefab1");
            ListItem2 itemScript = item.GetComponent<ListItem2>();
            if (item.IsInitHandlerCalled == false)
            {
                item.IsInitHandlerCalled = true;
              //  itemScript.Init();
            }
            //Debug.Log("itemName:" + itemData.mName);
            itemScript.SetItemData(itemData, index);
            return item;
        }

        void Update()
        {

        }
    }
}



using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NanoFrame.UI
{
    /// <summary>
    /// UI 窗体基类
    /// 作用：所有具体的 UI 面板（如 PauseMenu, ScoreBoard）都要继承它。
    /// </summary>
    public abstract class BaseUIForm : MonoBehaviour
    {
        // virtual（虚方法）的意思是：允许子类根据自己的需要，修改这个方法的具体行为

        /// <summary>
        /// 当界面被打开时执行
        /// </summary>
        public virtual void OnOpen()
        {
            gameObject.SetActive(true);
            // 这里可以放通用的逻辑，比如：每次打开界面都播放一个“唰”的音效
        }

        /// <summary>
        /// 当界面被关闭时执行
        /// </summary>
        public virtual void OnClose()
        {
            gameObject.SetActive(false);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NanoFrame.UI; // 引入 UI 框架

namespace Project.Gameplay
{
    // 继承 BaseUIForm，而不是 MonoBehaviour！
    public class PauseMenu : BaseUIForm
    {
        // 覆盖（override）父类的行为
        public override void OnOpen()
        {
            base.OnOpen(); // 依然执行父类的显示逻辑 gameObject.SetActive(true)

            // 业务逻辑：暂停游戏时间
            Time.timeScale = 0f;
            UnityEngine.Debug.Log("游戏已暂停！");
        }

        public override void OnClose()
        {
            base.OnClose();

            // 业务逻辑：恢复游戏时间
            Time.timeScale = 1f;
            UnityEngine.Debug.Log("游戏继续！");
        }

        // 绑定给 UI 上的“继续游戏”按钮
        public void OnResumeButtonClicked()
        {
            // 自己关闭自己！
            UIManager.Instance.CloseUI("PauseMenu");
        }
    }
}

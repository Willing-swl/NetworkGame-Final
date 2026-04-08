using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NanoFrame.Core; // 引入我们的单例基类

namespace NanoFrame.Audio
{
    /// <summary>
    /// 全局音频管理器
    /// </summary>
    public class AudioManager : Singleton<AudioManager>
    {
        [Header("音效喇叭")]
        private AudioSource _bgmSource; // 专门播背景音乐的喇叭
        private AudioSource _sfxSource; // 专门播短促音效的喇叭

        // 缓存字典：已经加载过的光盘(AudioClip)就不再去硬盘里找了
        private Dictionary<string, AudioClip> _audioClipDict = new Dictionary<string, AudioClip>();

        // 初始化时，给管家自动配齐两个喇叭
        private void Awake()
        {
            // 给当前游戏物体添加 AudioSource 组件
            _bgmSource = gameObject.AddComponent<AudioSource>();
            _sfxSource = gameObject.AddComponent<AudioSource>();

            // 背景音乐的专属设置
            _bgmSource.loop = true;  // 背景音乐必须循环播放
            _bgmSource.playOnAwake = false;

            // 音效的专属设置
            _sfxSource.loop = false; // 音效播完就停
            _sfxSource.playOnAwake = false;
        }

        /// <summary>
        /// 从 Resources 仓库里找光盘 (加载音频文件)
        /// </summary>
        private AudioClip LoadClip(string path)
        {
            // 如果字典里已经缓存了这张光盘，直接拿出来用
            if (_audioClipDict.TryGetValue(path, out AudioClip clip))
            {
                return clip;
            }

            // 如果没有，去 Resources 文件夹加载
            AudioClip newClip = Resources.Load<AudioClip>(path);
            if (newClip != null)
            {
                _audioClipDict.Add(path, newClip); // 存入字典缓存
            }
            else
            {
                Debug.LogError($"找不到音频文件！路径: Resources/{path}");
            }

            return newClip;
        }

        /// <summary>
        /// 播放背景音乐 (BGM)
        /// </summary>
        /// <param name="bgmName">BGM 文件的名字</param>
        public void PlayBGM(string bgmName)
        {
            AudioClip clip = LoadClip("Audio/BGM/" + bgmName);
            if (clip != null)
            {
                _bgmSource.clip = clip; // 把光盘塞进 BGM 喇叭
                _bgmSource.Play();      // 按下播放键
            }
        }

        /// <summary>
        /// 停止背景音乐
        /// </summary>
        public void StopBGM()
        {
            _bgmSource.Stop();
        }

        /// <summary>
        /// 播放音效 (SFX)
        /// </summary>
        /// <param name="sfxName">音效 文件的名字</param>
        public void PlaySFX(string sfxName)
        {
            AudioClip clip = LoadClip("Audio/SFX/" + sfxName);
            if (clip != null)
            {
                // PlayOneShot 的神妙之处：
                // 它允许同一个喇叭同时发出多个声音，不会互相打断！
                // 非常适合连发子弹、连续踩中墨水的场景。
                _sfxSource.PlayOneShot(clip);
            }
        }
    }
}

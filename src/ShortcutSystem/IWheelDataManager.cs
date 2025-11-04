using Duckov;
using ItemStatsSystem;
using ItemStatsSystem.Items;

namespace Backpack_QuickWheel.ShortcutSystem
{
    /// <summary>
    /// 轮盘数据管理器接口
    /// 定义轮盘系统的数据管理规范，支持配件系统和主背包两种模式的切换
    /// </summary>
    public interface IWheelDataManager
    {
        /// <summary>
        /// 初始化管理器
        /// 订阅必要事件，建立初始数据映射关系
        /// </summary>
        void Initialize();

        /// <summary>
        /// 关闭管理器
        /// 取消事件订阅，清理资源
        /// </summary>
        void Shutdown();

        /// <summary>
        /// 处理背包变化事件
        /// 当玩家更换背包时调用，用于更新轮盘数据源
        /// </summary>
        /// <param name="backpack">新的背包物品</param>
        void HandleBackpackChange(Item backpack);

        /// <summary>
        /// 游戏开始时调用
        /// 用于初始化轮盘显示数据
        /// </summary>
        void OnGameStart();

        /// <summary>
        /// 获取管理器类型描述
        /// 用于调试和日志记录
        /// </summary>
        /// <returns>管理器类型字符串</returns>
        string GetManagerType();
    }
}
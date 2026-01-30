



### 组件
- IMoveActuator 移动执行器
- IMoveGroupExplorer 移动组导出器
- IStateHasher   盘面状态hash生成
- IGoalStateChecker 最终盘面确定

- Filter
- Scorer
- IScoreCalibrator
- Selector
- SideEffect


### 功能模块
- 求解器：
  - 通用求解器 CommonSolver
    - IMoveGroupExplorer
    - IStateHasher
    - IGoalStateChecker
    - IMoveActuator
    - ObstacelUpdater
    - ObstacelPipline
  - TBS 求解器
    - 顶部边界表
    - TBS 展开序列器
  
- 障碍物建议器
- 难度求解器
  - IMoveActuator 移动执行器
  - IScoreSelector 打分组件
  - IGoalStateChecker
  - IMoveGenerator 生成当前所有合法的一步移动
  - stop one => record 一条记录
  - Difficulty Record
    - Move Sequence: List[Move with Score]
    - success: bool 
    - stepCount: int

### 需要面对的情况
##### 核心中的核心: 求解器 => MoveList
- 等长无障碍物
- 不等长无障碍物
- 等长有障碍物
- 不等长有障碍物

##### 障碍物
- 问号
- 帷幕
- 固定瓶
- 石膏
- 柜子和钥匙
- 冰盒
- 卷轴

##### 障碍物挂载建议（需要分析盘面的那种，提供辅助信息)
- 帷幕 
  - 可指定颜色
  - 可指定区域
- 柜子和锁
  - 确定柜子，找锁可以放哪些位置以及难度值
  - 放锁，找柜子的区域

### 难度值评估
1. 计算： global Move List => Score List => Sum(ScoreList)
2. For one Step:
   - 当前盘面的所有移动
   - 评估移动的权重（如使用指数函数，注意大数小数的上下溢出，权重最好统一归一化处理）
   - 选择对应的移动
     你只要给一个 List<TubeMoveAbility> 或 List<MoveGroup>
3. scoreSelector 打分组件
提供一个 scoreSelector（怎么打分）
直接返回 抽样选中的 index / 对象
内部自动做 稳定 softmax + 轮盘赌采样
尽量少分配（优先用 stackalloc，大了才走 ArrayPool）

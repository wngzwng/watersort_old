### 道具的三层
1. 移动能力影响， 能力生成阶段，对基础生成的 TubeMoveAbility 惊醒修改
过滤层：对于即不能倒水有不能接水的瓶子过滤掉
2. 分类依据 
    mono的分类， ability.TubeIndex -> Obstacle => groupBy()
3. 道具更新
    state + move => obstacle updater


Entry（数据） + Options（策略） + Pipeline（执行） 分离

对应到你这里就是：

ObstacleEntry：关卡里有哪些障碍物（数据）

ObstacleDescriptor：障碍物是什么、有哪些规则维度（策略）

ObstaclePipeline：在 Explore/Apply/AfterApply/Hash/Normalize 各阶段统一调度（执行）

这就会让你的系统变成：

“solver 不认识任何具体障碍物”
“solver 只调用 pipeline”
“新增障碍物 = 新增 handler + descriptor + extra/runtime 类型”

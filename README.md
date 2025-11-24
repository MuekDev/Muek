# Muek

唉，DAW

## 有关`StartBeat`和`EndBeat`的计算问题

由于历史遗留问题，这两个变量的定义跟大芬一样难以理解

实际上Beat是以小节为单位的，以4/4为例子，`Beat = 0.25` 代表处于第1/4个小节位置

### 换算公式

一些变量：

* **B**: `clip.StartBeat` (节拍数)
* **K**: `BeatsPerBar` (缩放因子，通常是 4) (除此之外还有个在 `DataStateService.cs`中叫做`Subdivisions`)
* **SR**: `sampleRate` (例如 44100)
* **Ch**: `Channels` (目前写死是 2)
* **BPM**: `DataStateService.Bpm`

#### A. 从 `StartBeat` 转换到 `实际采样点索引 (Index)`

这是混音器定位音频数据写入位置的公式：

$$\text{Index} = \left( \frac{B}{BPM} \times 60 \right) \times SR \times K \times Ch$$

* **步骤 1**: `B / BPM * 60` = **音乐时长（秒）**
* **步骤 2**: `音乐时长 * SR` = **基础采样数（单声道）**
* **步骤 3**: `基础采样数 * K * Ch` = **实际混音缓冲区索引** (TrackView里写的逻辑)

#### B. 从 `StartBeat` 转换到 `实际播放时间 (Wall Clock Seconds)`

播放头（Playhead）在 UI 上对应的物理时间。

$$\text{Seconds}_{\text{Play}} = \left( \frac{B}{BPM} \times 60 \right) \times K$$

* **注意**：这里必须乘以 `K (BeatsPerBar)`，因为石山代码

#### C. 从 `实际采样点索引 (Index)` 反推回 `StartBeat`

$$B = \frac{\text{Index}}{Ch \times K \times SR} \times \frac{BPM}{60}$$

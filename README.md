# ✨ Depth-To-Particle Unity Renderer Feature

基于 URP 的 Unity 自定义渲染功能，实现深度图转粒子的效果。

---

## 📘 项目信息

- **Unity 版本**：`2022.3.53f1c1`
- **渲染管线**：Universal Render Pipeline (URP)
- **示例场景**：`Assets/Scenes/0.a.unity`

---

## 🎯 项目简介

本项目展示了一种基于深度信息的粒子生成技术。通过在目标物体上添加自定义 Pass，仅渲染特定区域的深度信息，并结合 Renderer Feature 和 Compute Shader，将这些像素转换为粒子用于可视化特效。

---

## 🧠 实现思路

1. **自定义 Shader Pass**
   - 为溶解物体添加一个额外的 Pass。
   - 该 Pass 仅渲染溶解区域的深度信息，其他像素被剔除。

2. **ScriptableRendererFeature 获取渲染目标**
   - 在 URP 中添加自定义 Renderer Feature。
   - 指定 Shader 的 Pass 名称和渲染的 Layer，通过 Blit 将渲染结果写入 RTHandle。

3. **Compute Shader 处理像素数据**
   - 在 Compute Shader 中对渲染纹理进行采样。
   - 通过筛选像素计算出世界空间坐标。
   - 将有效坐标输出至 Buffer 中。

4. **粒子生成**
   - 在 CPU 端异步获取粒子顶点数据。
   - 通过程序化方式渲染生成粒子效果。

---

## 🧪 效果展示

![粒子生成效果演示](Assets/Temp/Show.gif)



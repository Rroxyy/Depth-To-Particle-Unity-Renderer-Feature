# ✨ Depth-To-Particle Unity Renderer Feature

A custom rendering feature based on URP in Unity that converts a **custom-rendered depth map** into particle effects.

---

## 📘 Project Info

- **Unity Version**: `2022.3.53f1c1`
- **Render Pipeline**: Universal Render Pipeline (URP)
- **Demo Scene**: `Assets/Scenes/0.a.unity`

---

## 🎯 Project Overview

This project demonstrates a technique for generating particles based on depth information. By adding a custom Pass to target objects, it renders only the depth of specific areas. Combined with a Renderer Feature and a Compute Shader, these pixels are converted into particles for visual effects.

---

## 🧠 Implementation Overview

1. **Custom Shader Pass**
   - Adds an extra Pass to the dissolving object.
   - This Pass renders only the depth information of the dissolving area, discarding other pixels.

2. **ScriptableRendererFeature to Capture Render Target**
   - A custom Renderer Feature is added in URP.
   - Specifies the Pass name and render Layer, writing the result to an RTHandle.

3. **Compute Shader for Pixel Processing**
   - Samples the render texture in the Compute Shader.
   - Filters pixels and computes world space coordinates.
   - Outputs valid coordinates to a buffer.

4. **Particle Generation**
   - Retrieves particle vertex data asynchronously on the CPU.
   - Procedurally renders particles based on the data.

---

## 🧪 Effect Preview

![Particle Generation Demo](./Assets/Temp/Show.gif)

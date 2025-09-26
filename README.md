# 🧠 Neural Network Denoiser SRP Unity

A real-time Unity application that combines **Path Tracing rendering** with a **neural network-based denoiser** to enhance image quality. It leverages a custom **Scriptable Render Pipeline (SRP)** and **Compute Shaders**, integrating a pre-trained **CNN model** via **Barracuda** for fast, GPU-accelerated inference.

---

## 🚀 Key Features

- Real-time **Path Tracing** rendering using Compute Shaders
- **Neural denoising** via a pre-trained CNN model (ONNX format)
- **Barracuda integration** for efficient inference in Unity
- **Synthetic training dataset** generated from Blender scenes
- Adjustable **samples-per-pixel** for performance vs. quality
- Designed to run on **legacy hardware** (e.g., GTX 970)

---

## 🧰 Technologies Used

| Technology        | Purpose                                           |
|-------------------|---------------------------------------------------|
| Unity 5 + SRP     | Graphics engine and custom render pipeline        |
| Compute Shader    | Real-time path tracing                            |
| TensorFlow        | CNN model training                                |
| ONNX              | Model conversion for Unity                        |
| Barracuda         | Neural inference engine                           |
| Graphy            | Real-time performance monitoring                  |
| TextMesh Pro      | Advanced UI rendering                             |

---

## 📁 Project Structure

```plaintext
Project Presentation/    # PDF presentation of the project
Assets/
├── Scenes/              # Unity scenes
├── ComputeShaders/      # Path tracing shaders
├── CustomRP/            # Custom SRP components
├── MODELS/              # 3D models (not included in repo)
├── NNMODELS/            # ONNX models for denoising
├── Scripts/             # C# scripts for camera, Barracuda, UI, etc.

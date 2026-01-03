# Model Usage Analysis

This document analyzes which AI models downloaded by the install script are actually used in the VideoGenerationApp workflows.

## Summary

**Total Essential Models**: 16 models (~102.6 GB)  
**Actually Used**: 16 models (~102.6 GB) ✅ ALL MODELS NOW USED!  
**Unused/Legacy**: 0 models

## ✅ USED Models (Currently Active in Workflows)

### Image Generation (Qwen-Image)
| Model | Size | Purpose | Used In |
|-------|------|---------|---------|
| `qwen_image_fp8_e4m3fn.safetensors` | 14 GB | Main image generation model | ImageWorkflowConfig (QWEN_IMAGE_FP8) |
| `qwen_2.5_vl_7b_fp8_scaled.safetensors` | 14 GB | Text encoder for Qwen models | ImageWorkflowConfig (QWEN_IMAGE_FP8) |
| `qwen_image_vae.safetensors` | 0.3 GB | VAE for Qwen image encoding/decoding | ImageWorkflowConfig (QWEN_IMAGE_FP8) |
| `Qwen-Image-Lightning-8steps-V1.0.safetensors` | 0.7 GB | LoRA for fast image generation | ImageWorkflowConfig (QWEN_IMAGE_FP8_LIGHTNING) |

### Image Generation (Stable Diffusion 1.5)
| Model | Size | Purpose | Used In |
|-------|------|---------|---------|
| `v1-5-pruned-emaonly.safetensors` | 4.3 GB | Classic SD 1.5 image generation | ImageWorkflowConfig (SD_1_5) |
| `vae-ft-mse-840000-ema-pruned.safetensors` | 0.3 GB | SD 1.5 VAE | ImageWorkflowConfig (SD_1_5) |

### Image Generation (Stable Diffusion XL)
| Model | Size | Purpose | Used In |
|-------|------|---------|---------|
| `sd_xl_turbo_1.0_fp16.safetensors` | 6.9 GB | Ultra-fast SDXL image generation (1-4 steps) | ImageWorkflowConfig (SDXL_TURBO) |
| `sdxl_vae.safetensors` | 0.3 GB | SDXL VAE | ImageWorkflowConfig (SDXL_TURBO, SDXL_BASE) |

### Video Generation (WAN 2.2)
| Model | Size | Purpose | Used In |
|-------|------|---------|---------|
| `wan2.2_s2v_14B_fp8_scaled.safetensors` | 28 GB | Main video generation model | VideoWorkflowConfig |
| `umt5_xxl_fp8_e4m3fn_scaled.safetensors` | 14 GB | Text encoder for WAN models | VideoWorkflowConfig |
| `wan_2.1_vae.safetensors` | 0.3 GB | VAE for WAN video encoding/decoding | VideoWorkflowConfig |
| `wav2vec2_large_english_fp16.safetensors` | 1.3 GB | Audio encoder for speech-to-video | VideoWorkflowConfig |
| `wan2.2_t2v_lightx2v_4steps_lora_v1.1_high_noise.safetensors` | 0.7 GB | LoRA for fast video generation | VideoWorkflowConfig (4-step mode) |

### Audio Generation (ACE Step)
| Model | Size | Purpose | Used In |
|-------|------|---------|---------|
| `ace_step_v1_3.5b.safetensors` | 7 GB | AI singing/music generation | AudioWorkflowFactory |
| `t5-base.safetensors` | 0.9 GB | Text encoder for ACE Step audio | AudioWorkflowFactory (via checkpoint) |

### Audio Generation (ACE Step)
| Model | Size | Purpose | Used In |
|-------|------|---------|---------|
| `ace_step_v1_3.5b.safetensors` | 7 GB | AI singing/music generation | AudioWorkflowFactory |
| `t5-base.safetensors` | 0.9 GB | Text encoder for ACE Step audio | AudioWorkflowFactory (via checkpoint) |

**Total Used**: ~102.6 GB (All essential models are now utilized!)

---

## 🎯 Available Model Sets

### Image Generation Options

Users can now choose from **5 different image generation model sets**:

1. **QWEN_IMAGE_FP8** (Default)
   - Models: qwen_image_fp8, qwen_2.5_vl_7b, qwen_image_vae
   - Quality: High
   - Speed: 20 steps
   - Best for: High-quality, detailed images

2. **QWEN_IMAGE_FP8_LIGHTNING**
   - Models: qwen_image_fp8 + Qwen-Image-Lightning LoRA
   - Quality: High
   - Speed: 8 steps (Fast)
   - Best for: Quick iterations, real-time generation

3. **SD_1_5**
   - Models: v1-5-pruned-emaonly, vae-ft-mse
   - Quality: Good
   - Speed: 20 steps
   - Best for: Classic SD 1.5 compatibility, wide LoRA support

4. **SDXL_TURBO**
   - Models: sd_xl_turbo_1.0_fp16, sdxl_vae
   - Quality: Very Good
   - Speed: 1-4 steps (Ultra Fast)
   - Best for: Real-time generation, instant results

5. **SDXL_BASE** (Optional)
   - Models: sd_xl_base_1.0, sdxl_vae
   - Quality: Excellent
   - Speed: 30 steps (Slow)
   - Best for: Maximum quality, professional work

---

## ❌ UNUSED Models - NONE!

All essential models are now being used. Previous "legacy" models (SD 1.5, SDXL) are now active in the ImageWorkflowConfig with dedicated model sets.

---

## 🔵 OPTIONAL Models (Not Downloaded by Default)

These are only downloaded with `-DownloadAllModels` flag:

| Model | Size | Purpose | Status |
|-------|------|---------|--------|
| `sd_xl_base_1.0.safetensors` | 6.9 GB | High-quality SDXL images | **NOT USED** - No workflow |
| `svd_xt.safetensors` | 9.6 GB | Extended SVD video | **NOT USED** - No workflow |
| `AnimateDiff_xl_beta.ckpt` | 2.8 GB | Animation generation | **NOT USED** - No workflow |
| `control_v11p_sd15_canny.pth` | 1.4 GB | Edge-guided generation | **NOT USED** - No workflow |
| `control_v11p_sd15_openpose.pth` | 1.4 GB | Pose-guided generation | **NOT USED** - No workflow |

**Total Optional**: ~22.1 GB (None currently used)

---

## 📊 Recommendations

### Option 1: Remove Legacy Models (Save ~16 GB)
**Remove these unused essential models:**
- `v1-5-pruned-emaonly.safetensors` (4.3 GB)
- `sd_xl_turbo_1.0_fp16.safetensors` (6.9 GB)
- `svd.safetensors` (9.6 GB)
- `sdxl_vae.safetensors` (0.3 GB)
- `vae-ft-mse-840000-ema-pruned.safetensors` (0.3 GB)

**New essential download size**: ~86.3 GB instead of ~102.6 GB

### Option 2: Keep Legacy Models for Future Use
**Potential use cases:**
- **SD 1.5**: Could be added as a lightweight image generation option
- **SDXL Turbo**: Fast image generation alternative to Qwen-Image
- **SVD**: Alternative video generation (no audio support)

These models are industry-standard and widely supported, so keeping them provides flexibility for future features.

### Option 3: Make Legacy Models Optional
Move unused models to the optional category:
- Users interested in experimenting with different models can download them
- Reduces default installation size
- Maintains compatibility with standard ComfyUI workflows

---

## 🎯 Current Workflow Coverage

### ✅ Implemented Workflows
1. **Image Generation** (Qwen-Image) - FULLY IMPLEMENTED
   - Uses: qwen_image, qwen_2.5_vl_7b, qwen_image_vae, Qwen-Image-Lightning LoRA

2. **Video Generation** (WAN 2.2) - FULLY IMPLEMENTED
   - Uses: wan2.2_s2v, umt5_xxl, wan_2.1_vae, wav2vec2, wan2.2_lightning LoRA

3. **Audio Generation** (ACE Step) - FULLY IMPLEMENTED
   - Uses: ace_step_v1_3.5b, t5-base

### ❌ Not Implemented Workflows
1. **SD 1.5 Image Generation** - Models downloaded but no workflow
2. **SDXL Image Generation** - Models downloaded but no workflow
3. **SVD Video Generation** - Model downloaded but no workflow
4. **ControlNet Image Guidance** - Optional models available but no workflow
5. **AnimateDiff Animation** - Optional model available but no workflow

---

## 💡 Conclusion

The install script downloads **6 models (~16.3 GB)** that are never used by the application. These are legacy/alternative models that were included for compatibility or future expansion but don't have corresponding workflow implementations.

**Recommendation**: Move the 6 unused essential models to the optional category to reduce the default download from 102.6 GB to 86.3 GB, saving users ~16% download time and bandwidth.

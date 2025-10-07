# Video Generation Bug Fixes Summary

## Issues Identified from Error Logs

### Error 1: Model Not Available
```
"ckpt_name: 'svd_xt.safetensors' not in ['ace_step_v1_3.5b.safetensors', 'stable-audio-open-1.0.safetensors', 'v1-5-pruned-emaonly-fp16.safetensors']"
```

**Cause**: The Stable Video Diffusion (SVD) model was not installed on the ComfyUI server.

**Solution**: 
- Added comprehensive documentation in `VIDEO_GENERATION_SETUP.md`
- Added comments in code explaining SVD requirement
- Updated README.md with setup instructions
- Model can be installed via `scripts/install.ps1` or manually

### Error 2: Link Connection Mismatch
```
"samples, received_type(CONDITIONING) mismatch input_type(LATENT)"
"linked_node": ["4", 0]
```

**Cause**: Workflow link 8 was connecting Node 4 Output 0 (CONDITIONING) to Node 5 Input 0 (expecting LATENT).

**Before**:
```csharp
// Link 8: SVDSampler -> VAEDecode  
new object[] { 8, 4, 0, 5, 0, "LATENT" },  // WRONG: Output 0 is CONDITIONING, not LATENT
```

**After**:
```csharp
// Link 8: SVDSampler output 2 (latent) -> VAEDecode
new object[] { 8, 4, 2, 5, 0, "LATENT" },  // CORRECT: Output 2 is LATENT
```

**Fixed in**: `VideoGenerationApp/Dto/VideoWorkflowFactory.cs` line 45

### Error 3: Incorrect Widget Value
**Cause**: SVD_img2vid_Conditioning node was using CFGScale instead of AugmentationLevel.

The SVD_img2vid_Conditioning node expects these widget values:
1. width
2. height
3. video_frames
4. motion_bucket_id
5. **augmentation_level** (NOT CFGScale)
6. seed

**Before**:
```csharp
widgets_values = new object[] 
{ 
    config.Width,
    config.Height,
    numFrames,
    motionBucketId,
    config.CFGScale,  // WRONG: Should be augmentation_level
    config.Seed
}
```

**After**:
```csharp
widgets_values = new object[] 
{ 
    config.Width,
    config.Height,
    numFrames,
    motionBucketId,
    config.AugmentationLevel,  // CORRECT: Using augmentation_level
    config.Seed
}
```

**Changes Made**:
- Added `AugmentationLevel` property to `VideoWorkflowConfig.cs`
- Updated `CreateSVDSamplerNode()` to use `config.AugmentationLevel`
- Updated test to verify `AugmentationLevel` is correctly set

## Files Changed

1. **VideoGenerationApp/Dto/VideoWorkflowConfig.cs**
   - Added `AugmentationLevel` property with default value 0.0f
   - Added documentation comments about SVD requirement

2. **VideoGenerationApp/Dto/VideoWorkflowFactory.cs**
   - Fixed link 8 to connect output 2 instead of output 0
   - Changed widget value from CFGScale to AugmentationLevel

3. **VideoGenerationApp.Tests/Dto/VideoWorkflowFactoryTests.cs**
   - Updated test to use AugmentationLevel instead of CFGScale
   - Added assertion to verify AugmentationLevel is set correctly

4. **VIDEO_GENERATION_SETUP.md** (NEW)
   - Comprehensive setup guide for SVD model
   - Troubleshooting section
   - Installation options (automated and manual)

5. **README.md**
   - Added reference to VIDEO_GENERATION_SETUP.md
   - Updated installation notes about SVD

## Workflow Structure (After Fix)

```
1. ImageOnlyCheckpointLoader (svd_xt.safetensors)
   ├─> MODEL → SVD_img2vid_Conditioning
   ├─> CLIP_VISION → SVD_img2vid_Conditioning
   └─> VAE → VAEEncode, VAEDecode

2. LoadImage (input image)
   └─> IMAGE → VAEEncode, SVD_img2vid_Conditioning

3. VAEEncode
   └─> LATENT → SVD_img2vid_Conditioning

4. SVD_img2vid_Conditioning
   ├─> positive (CONDITIONING) → (unused in current workflow)
   ├─> negative (CONDITIONING) → (unused in current workflow)
   └─> latent (LATENT) → VAEDecode  ✅ FIXED

5. VAEDecode
   └─> IMAGE → SaveImage

6. SaveImage
   └─> Saves video frames
```

## Verification

### Tests
- ✅ All 180 tests passing
- ✅ VideoWorkflowFactoryTests verify correct workflow structure
- ✅ No breaking changes to existing functionality

### Expected Behavior After Fix

Once the SVD model is installed, the workflow should:
1. Load the SVD checkpoint correctly
2. Connect nodes with proper data types
3. Pass correct parameters to SVD_img2vid_Conditioning
4. Generate video successfully

### For Users

To resolve the errors:
1. Run `pwsh ./scripts/install.ps1` to install SVD model
2. Wait for download to complete (~10GB)
3. Restart ComfyUI
4. Verify model appears in checkpoint list
5. Try video generation again

## Technical Details

### SVD_img2vid_Conditioning Node

This node is part of the Stable Video Diffusion workflow and creates conditioning for video generation from a single image.

**Inputs**:
- clip_vision: CLIP vision encoder from SVD model
- init_image: Starting image for video
- vae: VAE from SVD model

**Outputs**:
- positive: Positive conditioning (not used in basic workflow)
- negative: Negative conditioning (not used in basic workflow)  
- latent: Latent representation for video generation

**Parameters**:
- width, height: Video dimensions
- video_frames: Total frames to generate
- motion_bucket_id: Controls motion amount (127-254)
- augmentation_level: Image conditioning strength (0.0-0.3)
- seed: Random seed for generation

### Why Output 2 (latent)?

The SVD_img2vid_Conditioning node outputs three values:
- Index 0: positive (CONDITIONING)
- Index 1: negative (CONDITIONING)
- Index 2: latent (LATENT)

VAEDecode expects a LATENT input, so we must connect output index 2.

The conditioning outputs (0 and 1) would be used if we had a separate sampler node in the workflow, but the current simplified workflow uses the latent output directly.

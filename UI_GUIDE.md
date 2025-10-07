# UI Screenshots and Visual Guide

This document provides a textual description of the new UI pages since screenshots cannot be generated in this environment.

## Navigation Menu

The navigation menu has been updated to include two new items:

```
Home
Ollama Models
Generate Audio
Generate Image      <- NEW
Generate Video      <- NEW
Generation Queue
```

The navigation icons are:
- Generate Image: 🖼️ (bi-image)
- Generate Video: 🎥 (bi-camera-video)

## Generate Image Page

**URL:** `/generate-image`

**Layout:** Two-column layout

### Information Section

**About Image Generation** (info alert):
- Explains that the page uses Stable Diffusion to generate images from text descriptions
- Quick tips: Higher steps = better quality but slower. CFG Scale controls prompt adherence. Use seed for reproducible results.
- Link to external documentation: [Learn more about parameters](https://stable-diffusion-art.com/know-these-important-parameters-for-stunning-ai-images/)

### Left Column (Configuration - col-lg-8)

**Image Generation Configuration Section:**

1. **Positive Prompt** (textarea, 4 rows)
   - Placeholder: "e.g., beautiful landscape, mountains, sunset, high quality, detailed"
   - Tooltip: "Describe what you want in the image. Be specific and use descriptive adjectives. Include quality tags like 'high quality', 'detailed', '8k' for better results."
   - Helper text: "Describe desired elements, style, quality, and details you want in the image."

2. **Negative Prompt** (textarea, 3 rows)
   - Placeholder: "e.g., ugly, blurry, low quality, distorted"
   - Tooltip: "Describe what you DON'T want in the image. Common exclusions: ugly, blurry, low quality, distorted, deformed, bad anatomy, watermark."
   - Helper text: "List unwanted elements, artifacts, or quality issues to avoid."

3. **Width** (number input)
   - Range: 512-2048, step: 64
   - Default: 1024
   - Tooltip: "Image width in pixels. Must be divisible by 64. Larger sizes need more memory and time. Standard: 512-1024. High-res: 1536-2048."
   - Helper text: "512-2048 pixels, in steps of 64."

4. **Height** (number input)
   - Range: 512-2048, step: 64
   - Default: 1024
   - Tooltip: "Image height in pixels. Must be divisible by 64. Larger sizes need more memory and time. Standard: 512-1024. High-res: 1536-2048."
   - Helper text: "512-2048 pixels, in steps of 64."

5. **Generation Steps** (number input)
   - Range: 1-150
   - Default: 20
   - Tooltip: "Number of denoising iterations. More steps = better quality and detail, but slower generation. Recommended: 20-50 for quality, 15-25 for speed."
   - Helper text: "Higher values improve quality but increase generation time. Default: 20."

6. **CFG Scale (Classifier Free Guidance)** (number input)
   - Range: 1-30, step: 0.5
   - Default: 7.0
   - Tooltip: "Controls how strictly the AI follows your prompt. Lower (1-6) = more creative/varied. Higher (8-15) = stricter prompt adherence. Very high (>20) may reduce quality."
   - Helper text: "Controls prompt adherence. Range: 1-30. Recommended: 7-12. Default: 7."

7. **Seed** (number input)
   - Default: -1 (random)
   - Tooltip: "Random seed for reproducibility. Use -1 for random results each time. Use a specific number to reproduce the same image with identical settings."
   - Helper text: "Set to -1 for random, or use a specific number to reproduce results."

8. **Sampler** (dropdown)
   - Options: Euler, Euler Ancestral, DPM 2, DPM 2 Ancestral, DPM++ 2M, DPM++ SDE
   - Default: euler
   - Tooltip: "Sampling algorithm affects image generation style and quality. Euler: Fast, good for most uses. DPM++: High quality, slower. Ancestral variants: More random/creative."
   - Helper text: "Different algorithms produce different styles. Euler is a good default choice."

**Action Buttons:**
- Primary button: "Generate Image" (with loading state)
- Secondary button: "Reset to Defaults"

**Ollama Scene Reference** (shown when OutputState.ParsedOutput exists):
- Card showing:
  - Visual Description
  - Narrative
  - Mood (tone and emotion)
- Button: "Use Visual Description as Prompt"

### Right Column (Preview - col-lg-4)

**Preview Section:**
- Card with placeholder text: "Generated image will appear here after generation completes"
- Instruction: "View in Generation Queue for status updates"

## Generate Video Page

**URL:** `/generate-video`

**Layout:** Two-column layout

### Left Column (Configuration - col-lg-8)

**Video Generation Configuration Section:**

1. **Text Prompt / Description** (textarea, 4 rows)
   - Placeholder: "e.g., A serene landscape with moving clouds and gentle wind"
   - Tooltip: "Describe what should happen in the video"

2. **Audio File** (dropdown, optional)
   - Options: Previously generated audio files
   - Shows: Task name and duration
   - Default: "-- No Audio --"

3. **Base Image** (dropdown, optional)
   - Options: Previously generated images
   - Shows: Task name
   - Default: "-- Generate from prompt --"

4. **Duration** (number input)
   - Range: 1-120, step: 0.5
   - Default: 10.0 seconds
   - Shows helper text when audio selected: "Will match audio duration: X seconds"

5. **Width** (number input)
   - Range: 512-2048, step: 64
   - Default: 1024

6. **Height** (number input)
   - Range: 512-2048, step: 64
   - Default: 1024

7. **FPS** (dropdown)
   - Options: 24 FPS (Film), 30 FPS (Standard), 60 FPS (Smooth)
   - Default: 30

8. **Animation Style** (dropdown)
   - Options: Static, Smooth, Dynamic
   - Default: Smooth

9. **Motion Intensity** (number input)
   - Range: 0-1, step: 0.1
   - Default: 0.5
   - Tooltip: "How much motion/animation (0.0 = none, 1.0 = maximum)"

10. **Generation Steps** (number input)
    - Range: 1-100
    - Default: 20

11. **CFG Scale** (number input)
    - Range: 1-20, step: 0.5
    - Default: 7.0

12. **Quality** (number input)
    - Range: 0-100
    - Default: 90
    - Tooltip: "Video compression quality (0-100, higher = better)"

**Action Buttons:**
- Primary button: "Generate Video" (with loading state)
- Secondary button: "Reset to Defaults"

**Ollama Scene Reference** (shown when OutputState.ParsedOutput exists):
- Card showing:
  - Narrative
  - Visual Description
  - Mood
  - Suggested Actions (if available)
- Button: "Use Scene as Prompt"

### Right Column (Info - col-lg-4)

**Info Section:**
- Card showing current configuration:
  - Selected Audio: Name or "None"
  - Selected Image: Name or "None"
  - Estimated Duration: X seconds
  - Resolution: W x H
  - Frame Rate: X FPS

**Tip Alert (info):**
- Icon: 💡
- Message: "Generate audio and images first, then combine them here to create a complete video."

## Styling

Both pages use the existing application styles:
- Bootstrap 5 components (cards, forms, buttons, alerts)
- Bootstrap Icons for visual elements
- Custom `.parameter-section` class for consistent section styling
- Responsive layout with Bootstrap grid system
- Form controls with tooltips for user guidance
- Loading states on buttons during generation

## User Experience Flow

1. User navigates to Generate Image or Generate Video page
2. Form is pre-populated with Ollama output if available (via "Use" buttons)
3. User adjusts parameters as needed
4. User clicks "Generate" button
5. Alert confirms task has been queued
6. User can navigate to Generation Queue to monitor progress
7. When complete, generated files are accessible through the queue

## Color Scheme

The pages follow the existing application color scheme:
- Primary color for main action buttons
- Secondary color for reset/cancel actions
- Info alerts use light blue background
- Error messages use danger/red styling
- Success states use green/success styling

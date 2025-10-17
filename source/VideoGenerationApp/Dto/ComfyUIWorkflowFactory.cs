namespace VideoGenerationApp.Dto
{
    /// <summary>
    /// Factory for creating ComfyUI workflows from existing configurations
    /// </summary>
    public static class ComfyUIWorkflowFactory
    {
        /// <summary>
        /// Convert existing VideoWorkflowConfig to the new ComfyUI workflow DTO format
        /// </summary>
        public static ComfyUIWorkflowDto CreateFromVideoConfig(VideoWorkflowWrapper wrapper)
        {
            var workflow = new ComfyUIWorkflowDto
            {
                Id = Guid.NewGuid().ToString(),
                Version = 0.4,
                LastNodeId = 8,
                LastLinkId = 10
            };

            // Create nodes based on the current video workflow
            CreateLoadImageNode(workflow, wrapper);
            CreateCheckpointLoaderNode(workflow, wrapper);
            CreateVAEEncodeNode(workflow);
            CreateKSamplerNode(workflow, wrapper);
            CreatePositivePromptNode(workflow, wrapper);
            CreateNegativePromptNode(workflow, wrapper);
            CreateVAEDecodeNode(workflow);
            CreateSaveImageNode(workflow, wrapper);

            // Create links between nodes
            CreateWorkflowLinks(workflow);

            return workflow;
        }

        /// <summary>
        /// Load a workflow template from the example JSON file
        /// </summary>
        public static async Task<ComfyUIWorkflowDto> LoadVideoExampleWorkflowAsync()
        {
            var examplePath = Path.Combine("Doc", "ComfyUI", "Example_Workflows", "video_example.json");
            
            if (!File.Exists(examplePath))
            {
                throw new FileNotFoundException($"Video example workflow not found at: {examplePath}");
            }

            var workflow = await ComfyUIWorkflowSerializer.FromFileAsync(examplePath);
            return workflow ?? throw new InvalidOperationException("Failed to load video example workflow");
        }

        /// <summary>
        /// Create a simplified workflow template for basic video generation
        /// </summary>
        public static ComfyUIWorkflowDto CreateBasicVideoWorkflow(VideoWorkflowWrapper? wrapper = null)
        {
            wrapper ??= new VideoWorkflowWrapper();
            return CreateFromVideoConfig(wrapper);
        }

        private static void CreateLoadImageNode(ComfyUIWorkflowDto workflow, VideoWorkflowWrapper wrapper)
        {
            var node = workflow.AddNode("LoadImage", 100, 100);
            node.Size = new List<double> { 315, 314 };
            node.Inputs.Add(new ComfyUIWorkflowNodeInput 
            { 
                Name = "image", 
                Type = "STRING", 
                Widget = new ComfyUIWorkflowWidget { Name = "image" } 
            });
            node.Outputs.Add(new ComfyUIWorkflowNodeOutput 
            { 
                Name = "IMAGE", 
                Type = "IMAGE", 
                Links = new List<int> { 1 } 
            });
            node.Outputs.Add(new ComfyUIWorkflowNodeOutput 
            { 
                Name = "MASK", 
                Type = "MASK" 
            });
            node.WidgetValues.Add(!string.IsNullOrEmpty(wrapper.ImageFilePath) 
                ? Path.GetFileName(wrapper.ImageFilePath) 
                : "your_image.png");
        }

        private static void CreateCheckpointLoaderNode(ComfyUIWorkflowDto workflow, VideoWorkflowWrapper wrapper)
        {
            var node = workflow.AddNode("CheckpointLoaderSimple", 100, 500);
            node.Size = new List<double> { 350, 100 };
            node.Inputs.Add(new ComfyUIWorkflowNodeInput 
            { 
                Name = "ckpt_name", 
                Type = "COMBO", 
                Widget = new ComfyUIWorkflowWidget { Name = "ckpt_name" } 
            });
            node.Outputs.Add(new ComfyUIWorkflowNodeOutput 
            { 
                Name = "MODEL", 
                Type = "MODEL", 
                Links = new List<int> { 3 } 
            });
            node.Outputs.Add(new ComfyUIWorkflowNodeOutput 
            { 
                Name = "CLIP", 
                Type = "CLIP", 
                Links = new List<int> { 4, 5 } 
            });
            node.Outputs.Add(new ComfyUIWorkflowNodeOutput 
            { 
                Name = "VAE", 
                Type = "VAE", 
                Links = new List<int> { 2, 10 } 
            });
            node.WidgetValues.Add(wrapper.CheckpointName ?? "sd_xl_base_1.0.safetensors");
        }

        private static void CreateVAEEncodeNode(ComfyUIWorkflowDto workflow)
        {
            var node = workflow.AddNode("VAEEncode", 500, 100);
            node.Size = new List<double> { 200, 46 };
            node.Inputs.Add(new ComfyUIWorkflowNodeInput 
            { 
                Name = "pixels", 
                Type = "IMAGE", 
                Link = 1 
            });
            node.Inputs.Add(new ComfyUIWorkflowNodeInput 
            { 
                Name = "vae", 
                Type = "VAE", 
                Link = 2 
            });
            node.Outputs.Add(new ComfyUIWorkflowNodeOutput 
            { 
                Name = "LATENT", 
                Type = "LATENT", 
                Links = new List<int> { 8 } 
            });
        }

        private static void CreateKSamplerNode(ComfyUIWorkflowDto workflow, VideoWorkflowWrapper wrapper)
        {
            var node = workflow.AddNode("KSampler", 800, 100);
            node.Size = new List<double> { 315, 262 };
            
            // Add inputs
            node.Inputs.Add(new ComfyUIWorkflowNodeInput { Name = "model", Type = "MODEL", Link = 3 });
            node.Inputs.Add(new ComfyUIWorkflowNodeInput { Name = "positive", Type = "CONDITIONING", Link = 6 });
            node.Inputs.Add(new ComfyUIWorkflowNodeInput { Name = "negative", Type = "CONDITIONING", Link = 7 });
            node.Inputs.Add(new ComfyUIWorkflowNodeInput { Name = "latent_image", Type = "LATENT", Link = 8 });
            node.Inputs.Add(new ComfyUIWorkflowNodeInput { Name = "seed", Type = "INT", Widget = new ComfyUIWorkflowWidget { Name = "seed" } });
            node.Inputs.Add(new ComfyUIWorkflowNodeInput { Name = "steps", Type = "INT", Widget = new ComfyUIWorkflowWidget { Name = "steps" } });
            node.Inputs.Add(new ComfyUIWorkflowNodeInput { Name = "cfg", Type = "FLOAT", Widget = new ComfyUIWorkflowWidget { Name = "cfg" } });
            node.Inputs.Add(new ComfyUIWorkflowNodeInput { Name = "sampler_name", Type = "COMBO", Widget = new ComfyUIWorkflowWidget { Name = "sampler_name" } });
            node.Inputs.Add(new ComfyUIWorkflowNodeInput { Name = "scheduler", Type = "COMBO", Widget = new ComfyUIWorkflowWidget { Name = "scheduler" } });
            node.Inputs.Add(new ComfyUIWorkflowNodeInput { Name = "denoise", Type = "FLOAT", Widget = new ComfyUIWorkflowWidget { Name = "denoise" } });

            // Add output
            node.Outputs.Add(new ComfyUIWorkflowNodeOutput { Name = "LATENT", Type = "LATENT", Links = new List<int> { 9 } });

            // Add widget values
            node.WidgetValues.AddRange(new object[]
            {
                wrapper.Seed,
                "randomize",
                wrapper.Steps,
                wrapper.CFGScale,
                wrapper.SamplerName,
                wrapper.Scheduler,
                wrapper.Denoise
            });
        }

        private static void CreatePositivePromptNode(ComfyUIWorkflowDto workflow, VideoWorkflowWrapper wrapper)
        {
            var node = workflow.AddNode("CLIPTextEncode", 500, 400);
            node.Size = new List<double> { 400, 200 };
            node.Title = "CLIP Text Encode (Positive Prompt)";
            node.Color = "#232";
            node.BackgroundColor = "#353";

            node.Inputs.Add(new ComfyUIWorkflowNodeInput { Name = "text", Type = "STRING", Widget = new ComfyUIWorkflowWidget { Name = "text" } });
            node.Inputs.Add(new ComfyUIWorkflowNodeInput { Name = "clip", Type = "CLIP", Link = 4 });
            node.Outputs.Add(new ComfyUIWorkflowNodeOutput { Name = "CONDITIONING", Type = "CONDITIONING", Links = new List<int> { 6 } });
            
            node.WidgetValues.Add(wrapper.TextPrompt);
        }

        private static void CreateNegativePromptNode(ComfyUIWorkflowDto workflow, VideoWorkflowWrapper wrapper)
        {
            var node = workflow.AddNode("CLIPTextEncode", 500, 650);
            node.Size = new List<double> { 400, 200 };
            node.Title = "CLIP Text Encode (Negative Prompt)";
            node.Color = "#223";
            node.BackgroundColor = "#335";

            node.Inputs.Add(new ComfyUIWorkflowNodeInput { Name = "text", Type = "STRING", Widget = new ComfyUIWorkflowWidget { Name = "text" } });
            node.Inputs.Add(new ComfyUIWorkflowNodeInput { Name = "clip", Type = "CLIP", Link = 5 });
            node.Outputs.Add(new ComfyUIWorkflowNodeOutput { Name = "CONDITIONING", Type = "CONDITIONING", Links = new List<int> { 7 } });
            
            node.WidgetValues.Add(wrapper.NegativePrompt);
        }

        private static void CreateVAEDecodeNode(ComfyUIWorkflowDto workflow)
        {
            var node = workflow.AddNode("VAEDecode", 1200, 100);
            node.Size = new List<double> { 200, 46 };

            node.Inputs.Add(new ComfyUIWorkflowNodeInput { Name = "samples", Type = "LATENT", Link = 9 });
            node.Inputs.Add(new ComfyUIWorkflowNodeInput { Name = "vae", Type = "VAE", Link = 10 });
            node.Outputs.Add(new ComfyUIWorkflowNodeOutput { Name = "IMAGE", Type = "IMAGE", Links = new List<int> { 11 } });
        }

        private static void CreateSaveImageNode(ComfyUIWorkflowDto workflow, VideoWorkflowWrapper wrapper)
        {
            var node = workflow.AddNode("SaveImage", 1200, 300);
            node.Size = new List<double> { 315, 270 };

            node.Inputs.Add(new ComfyUIWorkflowNodeInput { Name = "images", Type = "IMAGE", Link = 11 });
            node.Inputs.Add(new ComfyUIWorkflowNodeInput { Name = "filename_prefix", Type = "STRING", Widget = new ComfyUIWorkflowWidget { Name = "filename_prefix" } });

            node.WidgetValues.Add(wrapper.OutputFilename);
        }

        private static void CreateWorkflowLinks(ComfyUIWorkflowDto workflow)
        {
            // LoadImage -> VAEEncode (pixels)
            workflow.AddLink(1, 0, 3, 0, "IMAGE");
            // CheckpointLoader VAE -> VAEEncode
            workflow.AddLink(2, 2, 3, 1, "VAE");
            // CheckpointLoader MODEL -> KSampler
            workflow.AddLink(2, 0, 4, 0, "MODEL");
            // CheckpointLoader CLIP -> PositivePrompt
            workflow.AddLink(2, 1, 5, 1, "CLIP");
            // CheckpointLoader CLIP -> NegativePrompt
            workflow.AddLink(2, 1, 6, 1, "CLIP");
            // PositivePrompt -> KSampler
            workflow.AddLink(5, 0, 4, 1, "CONDITIONING");
            // NegativePrompt -> KSampler
            workflow.AddLink(6, 0, 4, 2, "CONDITIONING");
            // VAEEncode -> KSampler (latent_image)
            workflow.AddLink(3, 0, 4, 3, "LATENT");
            // KSampler -> VAEDecode
            workflow.AddLink(4, 0, 7, 0, "LATENT");
            // CheckpointLoader VAE -> VAEDecode
            workflow.AddLink(2, 2, 7, 1, "VAE");
            // VAEDecode -> SaveImage
            workflow.AddLink(7, 0, 8, 0, "IMAGE");
        }
    }
}
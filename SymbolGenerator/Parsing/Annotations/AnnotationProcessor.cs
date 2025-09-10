using Amethyst.Common.Diagnostics;
using Amethyst.SymbolGenerator.Parsing.Annotations.Comments;
using System.Reflection;

namespace Amethyst.SymbolGenerator.Parsing.Annotations
{
    public class AnnotationProcessor
    {
        public static Dictionary<AnnotationHandlerAttribute, Type> Handlers { get; private set; }
        public HashSet<ProcessedAnnotation> ProcessedAnnotations { get; } = [];
        public HashSet<ProcessedAnnotation> ResolvedAnnotations { get; } = [];

        static AnnotationProcessor()
        {
            var handlerTypes = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => !type.IsInterface && !type.IsAbstract)
                .Where(type => type.GetCustomAttribute<AnnotationHandlerAttribute>() is not null)
                .Where(type => typeof(AbstractAnnotationHandler).IsAssignableFrom(type));
            Handlers = handlerTypes
                .Select(type => (attr: type.GetCustomAttribute<AnnotationHandlerAttribute>()!, type))
                .ToDictionary(t => t.attr, t => t.type);
        }

        public static string GetOfficialTagForAlias(string tag)
        {
            string tagLower = tag.ToLowerInvariant();
            var handler = Handlers.FirstOrDefault(h => h.Key.Tags.Select(t => t.ToLowerInvariant()).Contains(tagLower));
            return handler.Key?.Tags[0] ?? tagLower;
        }

        public void ProcessAndResolve(IEnumerable<RawAnnotation> annotations)
        {
            foreach (var annotation in annotations)
            {
                Process(annotation);
            }

            foreach (var annotation in ProcessedAnnotations.Where(a => !a.Resolved))
            {
                try
                {
                    annotation.Resolve(this);
                }
                catch (Exception ex)
                {
                    Logger.Warn($"Failed to resolve annotation '{annotation}' at {annotation.Annotation.Location}: {ex.Message}");
                }
            }
        }

        private void Process(RawAnnotation annotation)
        {
            string tagLower = annotation.Tag.ToLowerInvariant();
            Type? handlerType = Handlers.FirstOrDefault(h => h.Key.Tags.Select(t => t.ToLowerInvariant()).Contains(tagLower)).Value;
            if (handlerType is null)
            {
                return;
            }

            AbstractAnnotationHandler handler = (AbstractAnnotationHandler)Activator.CreateInstance(handlerType, [this])!;
            ProcessedAnnotation processed;
            try
            {
                handler.CanHandle(annotation);
                processed = handler.Handle(annotation);
                annotation.Target.Annotations.Add(processed);
            }
            catch (UnhandledAnnotationException ex)
            {
                Logger.Warn($"Skipping annotation '{annotation}' at {annotation.Location}: {ex.Message}");
                return;
            }
            catch
            {
                return;
            }

            ProcessedAnnotations.Add(processed);
        }
    }
}

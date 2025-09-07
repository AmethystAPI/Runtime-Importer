using Amethyst.Common.Diagnostics;
using System.Reflection;

namespace Amethyst.SymbolGenerator.Parsing.Annotations
{
    public static class AnnotationProcessor
    {
        private static readonly Dictionary<string, IAnnotationHandler> Handlers = new();
        public static IReadOnlyDictionary<string, IAnnotationHandler> RegisteredHandlers => Handlers;

        static AnnotationProcessor()
        {
            var handlerTypes = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => !type.IsInterface && !type.IsAbstract)
                .Where(type => type.GetCustomAttribute<AnnotationHandlerAttribute>() is not null)
                .Where(type => typeof(IAnnotationHandler).IsAssignableFrom(type));
            foreach (var handlerType in handlerTypes)
            {
                var attribute = handlerType.GetCustomAttribute<AnnotationHandlerAttribute>();
                if (attribute is null)
                    continue;
                if (Handlers.ContainsKey(attribute.HandlesTag.ToLower()))
                    throw new InvalidOperationException($"Multiple handlers registered for tag '{attribute.HandlesTag}'.");
                if (Activator.CreateInstance(handlerType) is not IAnnotationHandler handlerInstance)
                    throw new InvalidOperationException($"Failed to create instance of handler type '{handlerType.FullName}'.");
                Handlers[attribute.HandlesTag.ToLower()] = handlerInstance;
            }
        }

        public static IEnumerable<ProcessedAnnotation> ProcessAnnotations(IEnumerable<RawAnnotation> annotations)
        {
            foreach (var annotation in annotations)
            {
                if (ProcessAnnotation(annotation) is { } processed)
                    yield return processed;
            }
        }

        public static ProcessedAnnotation? ProcessAnnotation(RawAnnotation annotation)
        {
            string tagLower = annotation.Tag.ToLower();
            if (Handlers.TryGetValue(tagLower, out var handler))
            {
                try
                {
                    bool canApply;
                    Type handlerType = handler.GetType();
                    var handlerAtrib = handlerType.GetCustomAttribute<AnnotationHandlerAttribute>()!;

                    if (handlerAtrib.CollidesWithSelf && annotation.Target.HandledAnnotations.Contains(tagLower))
                    {
                        Logger.Warn($"Annotation with tag '{tagLower}' has already been applied to '{annotation.Target.FullName}'. Skipping duplicate.");
                        return null;
                    }

                    foreach (var collidingTag in handlerAtrib.CollidesWith)
                    {
                        if (annotation.Target.HandledAnnotations.Contains(collidingTag.ToLower()))
                        {
                            Logger.Warn($"Annotation with tag '{tagLower}' cannot be applied to '{annotation.Target.FullName}' because it collides with previously applied annotation '{collidingTag}'.");
                            return null;
                        }
                    }

                    try
                    {
                        canApply = handler.CanApply(annotation);
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn(ex.Message);
                        canApply = false;
                    }
                    if (!canApply)
                        return null;

                    var result = handler.Handle(annotation);

                    if (result is null)
                        return null;
                    var processed = new ProcessedAnnotation(annotation, result);
                    Logger.Info($"Processed annotation: {processed.Annotation} for {processed.Target.FullName}");
                    return processed;
                }
                catch (Exception ex)
                {
                    Logger.Warn($"Error processing annotation with tag '{tagLower}' for '{annotation.Target.FullName}': \n    {ex.Message}\n    at {annotation.Location}");
                }
            }
            else
            {
                //Logger.Warning($"No handler registered for annotation tag '{annotation.Tag}'.");
            }
            return null;
        }
    }
}

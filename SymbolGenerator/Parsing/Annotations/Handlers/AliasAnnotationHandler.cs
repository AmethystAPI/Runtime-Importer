using Amethyst.Common.Models;
using Amethyst.Common.Utility;
using Amethyst.SymbolGenerator.Parsing.Annotations.Comments;
using Amethyst.SymbolGenerator.Parsing.Annotations.ParameterPacks;

namespace Amethyst.SymbolGenerator.Parsing.Annotations.Handlers {
    [AnnotationHandler("alias", ["a"])]
    public class AliasAnnotationHandler(AnnotationProcessor processor, RawAnnotation annotation) : AbstractAnnotationHandler(processor, annotation) {
        public AliasAnnotationParameterPack ParameterPack { get; } = new AliasAnnotationParameterPack(annotation).Parse();

        public override HandlerAction CanHandle(RawAnnotation annotation) {
            return HandlerAction.Handle;
        }

        public override ProcessedAnnotation Handle(RawAnnotation annotation) {
            annotation.Target.Aliases.Add(ParameterPack.Alias);
            return new ProcessedAnnotation(
                annotation,
                new(annotation.Tag, ParameterPack.Platform),
                ParameterPack.Alias
            );
        }
    }
}

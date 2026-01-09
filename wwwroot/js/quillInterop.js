window.quillInterop = {
    instances: {},

    initialize: function (editorId, dotNetHelper, placeholder) {
        try {
            const container = document.getElementById(editorId);
            if (!container) {
                console.error('Quill container not found:', editorId);
                return false;
            }

            // Initialize Quill
            const quill = new Quill(container, {
                theme: 'snow',
                placeholder: placeholder || 'Write your thoughts...',
                modules: {
                    toolbar: [
                        [{ 'header': [1, 2, 3, false] }],
                        ['bold', 'italic', 'underline', 'strike'],
                        [{ 'list': 'ordered' }, { 'list': 'bullet' }],
                        [{ 'color': [] }, { 'background': [] }],
                        ['blockquote', 'code-block'],
                        ['link'],
                        ['clean']
                    ]
                }
            });

            // Store instance
            this.instances[editorId] = quill;

            // Listen for text changes
            quill.on('text-change', function () {
                const html = quill.root.innerHTML;
                const text = quill.getText();
                const wordCount = text.trim().split(/\s+/).filter(w => w.length > 0).length;

                dotNetHelper.invokeMethodAsync('OnContentChanged', html, text, wordCount);
            });

            return true;
        } catch (error) {
            console.error('Quill initialization error:', error);
            return false;
        }
    },

    setContent: function (editorId, htmlContent) {
        const quill = this.instances[editorId];
        if (quill) {
            quill.root.innerHTML = htmlContent || '';
        }
    },

    getContent: function (editorId) {
        const quill = this.instances[editorId];
        return quill ? quill.root.innerHTML : '';
    },

    getText: function (editorId) {
        const quill = this.instances[editorId];
        return quill ? quill.getText() : '';
    },

    focus: function (editorId) {
        const quill = this.instances[editorId];
        if (quill) {
            quill.focus();
        }
    },

    dispose: function (editorId) {
        const quill = this.instances[editorId];
        if (quill) {
            delete this.instances[editorId];
        }
    }
};
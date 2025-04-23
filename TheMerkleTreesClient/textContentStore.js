import { defineStore } from 'pinia';

export const useTextContentStore = defineStore('textContent', {
  state: () => ({
    textContent: '',
    fileNameWithoutExtension: '',
    parentFolderId: null,
    
  }),
  actions: {
    setTextContent(newContent) {
      this.textContent = newContent;
    },
    setFileNameWithoutExtension(fileName) {
      this.fileNameWithoutExtension = fileName.replace(/\.[^/.]+$/, '');
    },
    setParentFolderId(id) {
      this.parentFolderId = id;
    },
  },
});

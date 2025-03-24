import { defineStore } from 'pinia';

export const useTextContentStore = defineStore('textContent', {
  state: () => ({
    textContent: '',
    fileNameWithoutExtension: '',
  }),
  actions: {
    setTextContent(newContent) {
      this.textContent = newContent;
    },
    setFileNameWithoutExtension(fileName) {
      this.fileNameWithoutExtension = fileName.replace(/\.[^/.]+$/, '');
    },
  },
});

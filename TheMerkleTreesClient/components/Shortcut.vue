<template>
    <div class="flex flex-col items-center p-2 cursor-pointer rounded transition-transform transform hover:scale-105" @click="navigate">
      <img :src="favicon" alt="favicon" class="w-10 h-10 mb-2 rounded opacity-85" />
      <span class="text-center text-xs text-light-gray">{{ title }}</span>
    </div>
  </template>
  
  <script>
  export default {
    props: {
      url: {
        type: String,
        required: true
      },
      title: {
        type: String,
        required: true
      }
    },
    computed: {
      favicon() {
        return `https://www.google.com/s2/favicons?sz=64&domain_url=${this.url}`;
      }
    },
    methods: {
      navigate() {
        window.open(this.url, '_blank');
      }
    }
  };
  </script>
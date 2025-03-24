<template>
    <div class="text-center text-blue">
      <div class="text-xl font-semibold">{{ currentTime }}</div>
      <div class="text-lg">{{ currentDate }}</div>
    </div>
  </template>
  
  <script>
  export default {
    data() {
      return {
        currentDate: '',
        currentTime: ''
      };
    },
    methods: {
      updateDateTime() {
        const now = new Date();
        const dateOptions = { 
          weekday: 'long', 
          year: 'numeric', 
          month: 'long', 
          day: 'numeric' 
        };
        const timeOptions = { 
          hour: '2-digit', 
          minute: '2-digit', 
          second: '2-digit' 
        };
        this.currentDate = now.toLocaleDateString('fr-FR', dateOptions);
        this.currentTime = now.toLocaleTimeString('fr-FR', timeOptions);
      }
    },
    mounted() {
      this.updateDateTime();
      setInterval(this.updateDateTime, 1000);
    }
  };
  </script>
  
  <style scoped>
  /* Ajoutez des styles personnalisés si nécessaire */
  </style>
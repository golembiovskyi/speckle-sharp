<template>
  <v-app>
    <v-app-bar app color="primary" dark dense>
      <v-app-bar-title>møpper</v-app-bar-title>
      <v-spacer></v-spacer>
      <v-btn small @click="clearAllSchemas">Clear All</v-btn>
    </v-app-bar>

    <v-main>
      <selection-info/>
      <local-objects/>
    </v-main>
  </v-app>
</template>

<script>
import SelectionInfo from '@/components/SelectionInfo';
import LocalObjects from '@/components/LocalObjects';

export default {
  name: 'App',
  components: {
    SelectionInfo, 
    LocalObjects
  },
  data: () => ({
    // NADA
  }),
  methods: {
    clearAllSchemas() {
      window.chrome.webview.postMessage({
        action: 'clear-schema-all'
      })
    }
  }
};
</script>

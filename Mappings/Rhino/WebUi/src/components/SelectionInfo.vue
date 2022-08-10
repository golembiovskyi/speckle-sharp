<template>
  <v-container v-if="selectionInfo.objIds.length !== 0 && selectionInfo.schemas && selectionInfo.schemas.length !== 0">
    <v-row dense>
      <v-col cols="12">      
        <schema-selector :schemas="schemas" :key="key"/>
      </v-col>
    </v-row>
  </v-container>
</template>

<script>
import { mapState } from 'vuex'
import SchemaSelector from '@/components/SchemaSelector.vue'

  export default {
    name: 'SelectionInfo',
    components: { SchemaSelector },
    computed:{
      ...mapState(['selectionInfo']),
      schemas() {
        return this.selectionInfo.schemas
      },
      key(){
        return JSON.stringify(this.selectionInfo)
      }
    },
    data: () => ({
    })
  }
</script>

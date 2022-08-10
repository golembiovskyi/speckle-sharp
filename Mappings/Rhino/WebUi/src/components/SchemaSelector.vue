<template>
  <v-card style="width: 100%;" class="rounded-lg">
    <v-alert  class="grey lighten-2 caption" dense v-show="existingSchemas && existingSchemas.length!=0">
      Some objects have existing schemas applied. Values will be overwritten.
    </v-alert>
    <div class="px-2 pt-2">
      <div class="caption grey--text mb-3">Current selection: {{schemas.length}} viable {{schemas.length > 1 ? 'schemas' : 'schema'}}</div>
      <v-select :items="schemas" item-text="Name" hide-details item-value="Name" v-model="selectedSchemaName" outlined label="Schema" />
    </div>
    <div class="px-2 my-4 pb-2">
      <div class="caption grey--text mb-3">Properties</div>
      <template v-if="currentSchema && currentSchema.Warnings">
        <v-alert type="warning">{{currentSchema.Warnings}}</v-alert>
      </template>
      <schema-form :params="schemaParams" @set-param="setParam"/>
      <v-btn block class="primary" small :disabled="!dirty" @click="applySchema">
        <v-icon small v-if="!dirty" class="mr-2">mdi-check</v-icon>{{dirty ? 'Apply' : 'Schema Applied'}}</v-btn>
    </div>
  </v-card>
</template>

{"schemaName":"Wall","objectId":"571192bf-039b-4f9f-bd11-ed603d32aa8f","baseOffset":0}

<script>
import { mapState } from 'vuex'
import SchemaForm from '@/components/SchemaForm.vue'

export default {
  name: 'SchemaSelector',
  components:{ SchemaForm },
  props:{
    schemas: {
      type: Array,
      default: () => []
    }
  },
  computed:{
    ...mapState(['selectionInfo']),
    existingSchemas() {
      return this.selectionInfo.existingSchemas
    },
    currentSchema() {
      return this.schemas.find(s => s.Name === this.selectedSchemaName)
    },
    schemaParams() {
      if(this.currentSchema) return this.currentSchema.Params
      return []
    }
  },
  watch:{
    selectedSchemaName () { this.dirty = true; this.currentParams = {} }
  },
  data: () => ({
    selectedSchemaName: null,
    dirty: false,
    currentParams: {}
  }),
  methods:{
    setParam({val, paramName}) {
      if(this.currentParams[paramName] !== val ) this.dirty = true
      this.currentParams[paramName] = val
    },
    applySchema() {
      window.chrome.webview.postMessage({
        action: 'set-schema',
        objectIds: this.selectionInfo.objIds,
        schema: {...this.currentParams, schemaName: this.selectedSchemaName }
      })
      this.dirty = false
    }
  },
  mounted(){
    if(this.schemas.length === 1 ) {
      this.selectedSchemaName = this.schemas[0].Name
    }
  }
}
</script>

<template>
  <div>
    <template v-if="param.Type==='MultiselectParam'">
      <v-select v-model="model" :items="param.Values" @change="val => setParam(val)" hide-details dense outlined :label="param.Name + (varies ? ' (varies)' : '')"/>
    </template>
      <template v-if="param.Type==='StringParam'">
      <v-text-field v-model="model" @input="val => setParamDebounced(val)" hide-details dense outlined :label="param.Name + (varies ? ' (varies)' : '')"></v-text-field>
    </template>
    <template v-if="param.Type==='DoubleParam'">
      <v-text-field v-model="model" type="number" @input="val => setParamDebounced(val)" hide-details dense outlined :label="param.Name + (varies ? ' (varies)' : '')"></v-text-field>
    </template>
    <template v-if="param.Type==='CheckboxParam'">
      <v-checkbox v-model="model" @change="val => setParam(val)" hide-details :label="param.Name + (varies ? ' (varies)' : '')"></v-checkbox>
    </template>
    <div class="caption mb-4">{{param.Description}}</div>
  </div>
</template>
<script>
import { debounce } from "debounce"
import { mapState } from 'vuex'

export default {
  props: {
    param: {
      type: Object,
      default: () => null
    },
  },
  computed:{
    ...mapState(['selectionInfo']),
    existingValue(){
      return null
    }
  },
  data: () => ({
    model: null,
    varies: false
  }),
  mounted() {
    console.log(this.selectionInfo.existingSchemas)
    if(this.selectionInfo.existingSchemas && this.selectionInfo.existingSchemas.length !== 0) {
      let same = true
      let previousValue = null
      for(let schemaWrapper of this.selectionInfo.existingSchemas) {
        if(!previousValue) {
          previousValue = schemaWrapper.schema[this.param.Name]
          continue
        }
        if(previousValue !== schemaWrapper.schema[this.param.Name]) {
          same = false
        }
      }
      if(same) {
        this.model = previousValue
        this.setParam(previousValue) //NOTE: Hack
      } else {
        this.varies = true
      }
    }
  },
  methods:{
    setParam(val) {
      this.$emit('set-param', { paramName: this.param.Name, val })
    },
    setParamDebounced: debounce(function(val) { this.setParam(val)}, 500)
  }
}
</script>
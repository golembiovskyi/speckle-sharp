<template>
  <div>
    <template v-for="param in params">
      <schema-form-param :param="param" :key="param.Name" @set-param="setParam"/>
    </template>
  </div>
</template>
<script>
import { debounce } from "debounce"
import SchemaFormParam from '@/components/SchemaFormParam.vue'

export default {
  components: { SchemaFormParam },
  props: {
    params: {
      type: Array,
      default: () => []
    },
  },
  methods:{
    setParam({val, paramName}) {
      console.log(val, paramName)
      this.$emit('set-param', { paramName, val })
    },
    setParamDebounced: debounce(function(val, paramName) { this.setParam(val, paramName)}, 500)
  }
}
</script>
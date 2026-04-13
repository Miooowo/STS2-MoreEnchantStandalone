---
name: 缺陷 / Bug
about: 用于提交 Bug 和异常问题
title: ''
labels: ''
assignees: ''

---

title: "[Bug] "
labels:
  - "bug"

body:
  - type: textarea
    id: summary
    attributes:
      label: 问题概述
      placeholder: 简要描述问题现象
    validations:
      required: true

  - type: textarea
    id: steps
    attributes:
      label: 复现步骤
      placeholder: |
        1. 打开 ...
        2. 点击 ...
        3. 看到 ...
    validations:
      required: true

  - type: textarea
    id: expected
    attributes:
      label: 预期结果
    validations:
      required: true

  - type: textarea
    id: actual
    attributes:
      label: 实际结果
    validations:
      required: true

  - type: textarea
    id: extra
    attributes:
      label: 其他信息
      description: 日志、截图、环境信息等
    validations:
      required: false

# pig-truffles-unity-agent
pig getting those truffles...


## Dependencies:

- Unity
- ml-agents

## Training:

```Bash
mlagents-learn trainer_config.yaml --curriculum=./curriculum/pig --run-id=<id> --train
```

## Visualize:

```Bash
tensorboard --logdir summaries
```



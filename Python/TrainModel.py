from transformers import AutoModelForCausalLM, AutoTokenizer, Trainer, TrainingArguments, TextDataset, DataCollatorForLanguageModeling
import os

def fine_tune_model(model_path, dataset_path, output_path="model_finetuned", epochs=3):
    progress_path = os.path.join(output_path, "progress.log")
    os.makedirs(output_path, exist_ok=True)

    def log(msg):
        print(msg)
        with open(progress_path, "a", encoding="utf-8") as f:
            f.write(msg + "\n")

    log(f"[TRAIN] Loading model from: {model_path}")
    tokenizer = AutoTokenizer.from_pretrained(model_path)
    model = AutoModelForCausalLM.from_pretrained(model_path)

    log(f"[TRAIN] Preparing dataset: {dataset_path}")
    dataset = TextDataset(
        tokenizer=tokenizer,
        file_path=dataset_path,
        block_size=128
    )

    data_collator = DataCollatorForLanguageModeling(
        tokenizer=tokenizer, mlm=False
    )

    args = TrainingArguments(
        output_dir=output_path,
        overwrite_output_dir=True,
        num_train_epochs=epochs,
        per_device_train_batch_size=2,
        save_steps=100,
        save_total_limit=2,
        logging_dir=os.path.join(output_path, "logs"),
        logging_steps=10
    )

    log(f"[TRAIN] Starting training for {epochs} epochs...")

    trainer = Trainer(
        model=model,
        args=args,
        data_collator=data_collator,
        train_dataset=dataset,
    )

    trainer.train()

    log(f"[TRAIN] Saving to: {output_path}")
    trainer.save_model(output_path)
    tokenizer.save_pretrained(output_path)

    log("[TRAIN] Done.")

from transformers import AutoModelForCausalLM, AutoTokenizer, Trainer, TrainingArguments, TextDataset, DataCollatorForLanguageModeling
import os
import logging
import sys
import traceback

# Configure logging
logging.basicConfig(level=logging.INFO,
                    format=' %(levelname)s - %(message)s',
                    stream=sys.stdout) 


def fine_tune_model(model_path, dataset_path, output_path="model_finetuned", epochs=3):
    progress_path = os.path.join(output_path, "progress.log")
    os.makedirs(output_path, exist_ok=True)

    def log(msg):
        logging.info(msg)
        try:
            with open(progress_path, "a", encoding="utf-8") as f:
                f.write(msg + "\n")
        except Exception as e:
            logging.error(f"Error writing to log file: {e}\n{traceback.format_exc()}")

    log(f"[TRAIN] Starting fine-tuning with:")
    log(f"  - Model path: {model_path}")
    log(f"  - Dataset path: {dataset_path}")
    log(f"  - Output path: {output_path}")
    log(f"  - Epochs: {epochs}")

    try:
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

    except FileNotFoundError:
        logging.error(f"[TRAIN] File not found: {model_path} or {dataset_path}\n{traceback.format_exc()}")
        print("Error: Model or dataset path not found. Check your paths.")
        sys.exit(1)
    except Exception as e:
        logging.error(f"[TRAIN] An error occurred during training: {e}\n{traceback.format_exc()}")
        print(f"Error during training: {e}")
        sys.exit(1)


if __name__ == "__main__":
    if len(sys.argv) != 5:
        print("Usage: python train_model.py <model_path> <dataset_path> <output_path> <epochs>")
        sys.exit(1)

    model_path = sys.argv[1]
    dataset_path = sys.argv[2]
    output_path = sys.argv[3]
    epochs = int(sys.argv[4])

    fine_tune_model(model_path, dataset_path, output_path, epochs)
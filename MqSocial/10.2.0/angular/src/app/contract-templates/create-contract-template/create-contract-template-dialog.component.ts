import { Component, Injector, EventEmitter, Output, ChangeDetectorRef, ViewChild, ElementRef } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BsModalRef } from 'ngx-bootstrap/modal';
import { AppComponentBase } from '@shared/app-component-base';
import { AppConsts } from '@shared/AppConsts';
import { ContractTemplateServiceProxy, CreateContractTemplateDto } from '@shared/service-proxies/service-proxies';
import { FormsModule } from '@angular/forms';
import { AbpModalHeaderComponent } from '../../../shared/components/modal/abp-modal-header.component';
import { AbpValidationSummaryComponent } from '../../../shared/components/validation/abp-validation.summary.component';
import { LocalizePipe } from '@shared/pipes/localize.pipe';
import { InputText } from 'primeng/inputtext';
import { Button } from 'primeng/button';

interface UploadContractTemplateFileResult {
    filePath: string;
    fileName: string;
}

@Component({
    templateUrl: './create-contract-template-dialog.component.html',
    standalone: true,
    imports: [
        FormsModule,
        AbpModalHeaderComponent,
        AbpValidationSummaryComponent,
        LocalizePipe,
        InputText,
        Button,
    ],
})
export class CreateContractTemplateDialogComponent extends AppComponentBase {
    @ViewChild('fileInput') fileInput!: ElementRef<HTMLInputElement>;
    @Output() onSave = new EventEmitter<any>();

    saving = false;
    contractTemplate = new CreateContractTemplateDto();
    selectedFile: File | null = null;

    constructor(
        injector: Injector,
        public _contractTemplateService: ContractTemplateServiceProxy,
        public bsModalRef: BsModalRef,
        private _http: HttpClient,
        private cd: ChangeDetectorRef
    ) {
        super(injector);
    }

    pickFile(): void {
        this.fileInput.nativeElement.click();
    }

    onFileSelected(event: Event): void {
        const input = event.target as HTMLInputElement;
        this.selectedFile = input.files?.[0] ?? null;
        this.cd.detectChanges();
    }

    save(): void {
        this.saving = true;

        if (this.selectedFile) {
            const formData = new FormData();
            formData.append('file', this.selectedFile);
            const url = `${AppConsts.remoteServiceBaseUrl}/api/services/app/ContractTemplate/UploadFile`;
            this._http.post<{ result: UploadContractTemplateFileResult }>(url, formData).subscribe({
                next: (response) => {
                    this.contractTemplate.filePath = response.result.filePath;
                    this.createContractTemplate();
                },
                error: () => { this.saving = false; this.cd.detectChanges(); },
            });
        } else {
            this.createContractTemplate();
        }
    }

    private createContractTemplate(): void {
        this._contractTemplateService.create(this.contractTemplate).subscribe(
            () => {
                this.notify.info(this.l('SavedSuccessfully'));
                this.bsModalRef.hide();
                this.onSave.emit();
            },
            () => { this.saving = false; this.cd.detectChanges(); }
        );
    }
}
